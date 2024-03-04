using NAudio.CoreAudioApi.Interfaces;
using NAudio.Wasapi.CoreAudioApi.Interfaces;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace NAudio.Wasapi.CoreAudioApi
{
    internal class ActivateAudioInterfaceCompletionHandler :
    IActivateAudioInterfaceCompletionHandler, IAgileObject
    {
        private Action<IAudioClient2> initializeAction;
        private TaskCompletionSource<IAudioClient2> tcs = new TaskCompletionSource<IAudioClient2>();

        public ActivateAudioInterfaceCompletionHandler(
            Action<IAudioClient2> initializeAction)
        {
            this.initializeAction = initializeAction;
        }

        /// <summary>
        /// Activates the completed audio interface asynchronous operation and handles any exceptions that occur during the process.
        /// </summary>
        /// <param name="activateOperation">The completed audio interface asynchronous operation to be activated.</param>
        /// <exception cref="Exception">Thrown when an error occurs during the activation process.</exception>
        /// <remarks>
        /// This method first retrieves the activation results from the <paramref name="activateOperation"/> and checks for any errors. If an error is found, it sets an exception using the <see cref="TaskCompletionSource{TResult}.TrySetException(Exception)"/> method.
        /// If no errors are found, it attempts to initialize the audio client using the provided <paramref name="initializeAction"/> and sets the result using the <see cref="TaskCompletionSource{TResult}.SetResult(TResult)"/> method.
        /// If an exception occurs during the initialization process, it sets the exception using the <see cref="TaskCompletionSource{TResult}.TrySetException(Exception)"/> method.
        /// </remarks>
        public void ActivateCompleted(IActivateAudioInterfaceAsyncOperation activateOperation)
        {
            // First get the activation results, and see if anything bad happened then
            activateOperation.GetActivateResult(out int hr, out object unk);
            if (hr != 0)
            {
                tcs.TrySetException(Marshal.GetExceptionForHR(hr, new IntPtr(-1)));
                return;
            }

            var pAudioClient = (IAudioClient2)unk;

            // Next try to call the client's (synchronous, blocking) initialization method.
            try
            {
                initializeAction(pAudioClient);
                tcs.SetResult(pAudioClient);
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }


        }

        /// <summary>
        /// Gets the awaiter for the asynchronous task and returns an awaiter for the <see cref="IAudioClient2"/>.
        /// </summary>
        /// <returns>An awaiter for the <see cref="IAudioClient2"/>.</returns>
        public TaskAwaiter<IAudioClient2> GetAwaiter()
        {
            return tcs.Task.GetAwaiter();
        }
    }
}
