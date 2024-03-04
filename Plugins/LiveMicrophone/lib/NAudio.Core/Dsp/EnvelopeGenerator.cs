using System;
using System.Linq;

namespace NAudio.Dsp
{
    // C# ADSR based on work by Nigel Redmon, EarLevel Engineering: earlevel.com
    // http://www.earlevel.com/main/2013/06/03/envelope-generators-adsr-code/
    /// <summary>
    /// Envelope generator (ADSR)
    /// </summary>
    public class EnvelopeGenerator
    {
        private EnvelopeState state;
        private float output;
        private float attackRate;
        private float decayRate;
        private float releaseRate;
        private float attackCoef;
        private float decayCoef;
        private float releaseCoef;
        private float sustainLevel;
        private float targetRatioAttack;
        private float targetRatioDecayRelease;
        private float attackBase;
        private float decayBase;
        private float releaseBase;

        /// <summary>
        /// Envelope State
        /// </summary>
        public enum EnvelopeState
        {
            /// <summary>
            /// Idle
            /// </summary>
            Idle = 0,
            /// <summary>
            /// Attack
            /// </summary>
            Attack,
            /// <summary>
            /// Decay
            /// </summary>
            Decay,
            /// <summary>
            /// Sustain
            /// </summary>
            Sustain,
            /// <summary>
            /// Release
            /// </summary>
            Release
        };

        /// <summary>
        /// Creates and Initializes an Envelope Generator
        /// </summary>
        public EnvelopeGenerator()
        {
            Reset();
            AttackRate = 0;
            DecayRate = 0;
            ReleaseRate = 0;
            SustainLevel = 1.0f;
            SetTargetRatioAttack(0.3f);
            SetTargetRatioDecayRelease(0.0001f);
        }

        /// <summary>
        /// Attack Rate (seconds * SamplesPerSecond)
        /// </summary>
        public float AttackRate
        {
            get
            {
                return attackRate;
            }
            set
            {
                attackRate = value;
                attackCoef = CalcCoef(value, targetRatioAttack);
                attackBase = (1.0f + targetRatioAttack) * (1.0f - attackCoef);
            }
        }

        /// <summary>
        /// Decay Rate (seconds * SamplesPerSecond)
        /// </summary>
        public float DecayRate
        {
            get
            {
                return decayRate;
            }
            set
            {
                decayRate = value;
                decayCoef = CalcCoef(value, targetRatioDecayRelease);
                decayBase = (sustainLevel - targetRatioDecayRelease) * (1.0f - decayCoef);
            }
        }

        /// <summary>
        /// Release Rate (seconds * SamplesPerSecond)
        /// </summary>
        public float ReleaseRate
        {
            get
            {
                return releaseRate;
            }
            set
            {
                releaseRate = value;
                releaseCoef = CalcCoef(value, targetRatioDecayRelease);
                releaseBase = -targetRatioDecayRelease * (1.0f - releaseCoef);
            }
        }

        /// <summary>
        /// Calculates the coefficient using the given rate and target ratio and returns the result.
        /// </summary>
        /// <param name="rate">The rate used in the calculation.</param>
        /// <param name="targetRatio">The target ratio used in the calculation.</param>
        /// <returns>The calculated coefficient based on the provided <paramref name="rate"/> and <paramref name="targetRatio"/>.</returns>
        /// <remarks>
        /// This method calculates the coefficient using the formula:
        /// coefficient = (float)Math.Exp(-Math.Log((1.0f + targetRatio) / targetRatio) / rate).
        /// </remarks>
        private static float CalcCoef(float rate, float targetRatio)
        {
            return (float)Math.Exp(-Math.Log((1.0f + targetRatio) / targetRatio) / rate);
        }

        /// <summary>
        /// Sustain Level (1 = 100%)
        /// </summary>
        public float SustainLevel
        {
            get
            {
                return sustainLevel;
            }
            set
            {
                sustainLevel = value;
                decayBase = (sustainLevel - targetRatioDecayRelease) * (1.0f - decayCoef);
            }
        }

        /// <summary>
        /// Sets the target ratio for attack and updates the attack base.
        /// </summary>
        /// <param name="targetRatio">The target ratio for attack.</param>
        /// <remarks>
        /// If the <paramref name="targetRatio"/> is less than 0.000000001f, it is set to 0.000000001f (-180 dB).
        /// The <paramref name="targetRatio"/> is then assigned to <see cref="targetRatioAttack"/> and the <see cref="attackBase"/> is updated using the formula: (1.0f + targetRatioAttack) * (1.0f - attackCoef).
        /// </remarks>
        void SetTargetRatioAttack(float targetRatio)
        {
            if (targetRatio < 0.000000001f)
                targetRatio = 0.000000001f;  // -180 dB
            targetRatioAttack = targetRatio;
            attackBase = (1.0f + targetRatioAttack) * (1.0f - attackCoef);
        }

        /// <summary>
        /// Sets the target ratio for decay and release and calculates the decay and release bases.
        /// </summary>
        /// <param name="targetRatio">The target ratio for decay and release.</param>
        /// <remarks>
        /// If the <paramref name="targetRatio"/> is less than 0.000000001f, it is set to 0.000000001f (-180 dB).
        /// The decay base is calculated as (sustainLevel - targetRatioDecayRelease) * (1.0f - decayCoef).
        /// The release base is calculated as -targetRatioDecayRelease * (1.0f - releaseCoef).
        /// </remarks>
        void SetTargetRatioDecayRelease(float targetRatio)
        {
            if (targetRatio < 0.000000001f)
                targetRatio = 0.000000001f;  // -180 dB
            targetRatioDecayRelease = targetRatio;
            decayBase = (sustainLevel - targetRatioDecayRelease) * (1.0f - decayCoef);
            releaseBase = -targetRatioDecayRelease * (1.0f - releaseCoef);
        }

        /// <summary>
        /// Processes the envelope state and returns the output value.
        /// </summary>
        /// <returns>The output value after processing the envelope state.</returns>
        /// <remarks>
        /// This method processes the envelope state based on the current state and coefficients for attack, decay, sustain, and release.
        /// It updates the output value according to the state transitions and coefficient calculations.
        /// The method returns the final output value after processing the envelope state.
        /// </remarks>
        public float Process()
        {
            switch (state)
            {
                case EnvelopeState.Idle:
                    break;
                case EnvelopeState.Attack:
                    output = attackBase + output * attackCoef;
                    if (output >= 1.0f)
                    {
                        output = 1.0f;
                        state = EnvelopeState.Decay;
                    }
                    break;
                case EnvelopeState.Decay:
                    output = decayBase + output * decayCoef;
                    if (output <= sustainLevel)
                    {
                        output = sustainLevel;
                        state = EnvelopeState.Sustain;
                    }
                    break;
                case EnvelopeState.Sustain:
                    break;
                case EnvelopeState.Release:
                    output = releaseBase + output * releaseCoef;
                    if (output <= 0.0)
                    {
                        output = 0.0f;
                        state = EnvelopeState.Idle;
                    }
                    break;
            }
            return output;
        }

        /// <summary>
        /// Sets the envelope state based on the gate condition.
        /// </summary>
        /// <param name="gate">The condition to set the envelope state. If true, sets the state to <see cref="EnvelopeState.Attack"/>; otherwise, sets the state to <see cref="EnvelopeState.Release"/> if the current state is not <see cref="EnvelopeState.Idle"/>.</param>
        /// <remarks>
        /// This method sets the envelope state based on the gate condition. If the gate is true, it sets the state to attack. If the gate is false and the current state is not idle, it sets the state to release.
        /// </remarks>
        public void Gate(bool gate)
        {
            if (gate)
                state = EnvelopeState.Attack;
            else if (state != EnvelopeState.Idle)
                state = EnvelopeState.Release;
        }

        /// <summary>
        /// Current envelope state
        /// </summary>
        public EnvelopeState State
        {
            get
            {
                return state;
            }
        }

        /// <summary>
        /// Resets the state of the envelope and sets the output to 0.0f.
        /// </summary>
        public void Reset()
        {
            state = EnvelopeState.Idle;
            output = 0.0f;
        }

        /// <summary>
        /// Gets the output value.
        /// </summary>
        /// <returns>The output value.</returns>
        public float GetOutput()
        {
            return output;
        }
    }
}
