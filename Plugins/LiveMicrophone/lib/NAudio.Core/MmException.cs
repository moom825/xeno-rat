using System;

namespace NAudio
{
    /// <summary>
    /// Summary description for MmException.
    /// </summary>
    public class MmException : Exception
    {
        private MmResult result;
        private string function;

        /// <summary>
        /// Creates a new MmException
        /// </summary>
        /// <param name="result">The result returned by the Windows API call</param>
        /// <param name="function">The name of the Windows API that failed</param>
        public MmException(MmResult result, string function)
            : base(MmException.ErrorMessage(result, function))
        {
            this.result = result;
            this.function = function;
        }

        /// <summary>
        /// Generates an error message based on the result and function name.
        /// </summary>
        /// <param name="result">The result of the function call.</param>
        /// <param name="function">The name of the function being called.</param>
        /// <returns>A string containing the error message with the result and function name.</returns>
        private static string ErrorMessage(MmResult result, string function)
        {
            return String.Format("{0} calling {1}", result, function);
        }

        /// <summary>
        /// Throws an exception if the specified result is not equal to MmResult.NoError.
        /// </summary>
        /// <param name="result">The result to be checked.</param>
        /// <param name="function">The name of the function where the exception is thrown.</param>
        /// <exception cref="MmException">Thrown when the specified result is not equal to MmResult.NoError.</exception>
        public static void Try(MmResult result, string function)
        {
            if (result != MmResult.NoError)
                throw new MmException(result, function);
        }

        /// <summary>
        /// Returns the Windows API result
        /// </summary>
        public MmResult Result
        {
            get
            {
                return result;
            }
        }
    }
}
