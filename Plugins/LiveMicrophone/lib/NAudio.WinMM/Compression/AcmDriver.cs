using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using NAudio.Utils;

namespace NAudio.Wave.Compression
{
    /// <summary>
    /// Represents an installed ACM Driver
    /// </summary>
    public class AcmDriver : IDisposable
    {
        private static List<AcmDriver> drivers;
        private AcmDriverDetails details;
        private IntPtr driverId;
        private IntPtr driverHandle;
        private List<AcmFormatTag> formatTags;
        private List<AcmFormat> tempFormatsList; // used by enumerator
        private IntPtr localDllHandle;

        /// <summary>
        /// Checks if a codec with the specified short name is installed.
        /// </summary>
        /// <param name="shortName">The short name of the codec to be checked.</param>
        /// <returns>True if a codec with the specified short name is installed; otherwise, false.</returns>
        public static bool IsCodecInstalled(string shortName)
        {
            foreach (AcmDriver driver in EnumerateAcmDrivers())
            {
                if (driver.ShortName == shortName)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Adds a local driver from the specified file and returns the driver information.
        /// </summary>
        /// <param name="driverFile">The path to the driver file to be added.</param>
        /// <exception cref="ArgumentException">Thrown when failed to load the driver file or discover DriverProc.</exception>
        /// <exception cref="MmException">Thrown when acmDriverAdd operation fails.</exception>
        /// <returns>The driver information for the added local driver.</returns>
        /// <remarks>
        /// This method loads the specified driver file using NativeMethods.LoadLibrary, then retrieves the address of the "DriverProc" function using NativeMethods.GetProcAddress.
        /// It then adds the driver using AcmInterop.acmDriverAdd, and if successful, creates and returns an AcmDriver object with the driver information.
        /// If the long name of the driver is missing, it is set to "Local driver: " followed by the name of the driver file.
        /// </remarks>
        public static AcmDriver AddLocalDriver(string driverFile)
        {
            IntPtr handle = NativeMethods.LoadLibrary(driverFile);
            if (handle == IntPtr.Zero)
            {
                throw new ArgumentException("Failed to load driver file");
            }
            var driverProc = NativeMethods.GetProcAddress(handle, "DriverProc");
            if (driverProc == IntPtr.Zero)
            {
                NativeMethods.FreeLibrary(handle);
                throw new ArgumentException("Failed to discover DriverProc");
            }
            var result = AcmInterop.acmDriverAdd(out IntPtr driverHandle,
                handle, driverProc, 0, AcmDriverAddFlags.Function);
            if (result != MmResult.NoError)
            {
                NativeMethods.FreeLibrary(handle);
                throw new MmException(result, "acmDriverAdd");
            }
            var driver = new AcmDriver(driverHandle);
            // long name seems to be missing when we use acmDriverAdd
            if (string.IsNullOrEmpty(driver.details.longName))
            {
                driver.details.longName = "Local driver: " + Path.GetFileName(driverFile);
                driver.localDllHandle = handle;
            }
            return driver;
        }

        /// <summary>
        /// Removes the local driver and frees the associated resources.
        /// </summary>
        /// <param name="localDriver">The AcmDriver to be removed.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="localDriver"/> has an invalid or uninitialized handle.</exception>
        /// <remarks>
        /// This method removes the local driver identified by the <paramref name="localDriver"/> and frees the associated resources.
        /// It first checks if the <paramref name="localDriver"/> has a valid handle, and if not, it throws an <see cref="ArgumentException"/>.
        /// After removing the driver, it frees the library associated with the local driver's handle using <see cref="NativeMethods.FreeLibrary"/>.
        /// Finally, it attempts to remove the driver using <see cref="AcmInterop.acmDriverRemove"/> and handles any exceptions using <see cref="MmException.Try"/>.
        /// </remarks>
        public static void RemoveLocalDriver(AcmDriver localDriver)
        {
            if (localDriver.localDllHandle == IntPtr.Zero)
            {
                throw new ArgumentException("Please pass in the AcmDriver returned by the AddLocalDriver method");
            }
            var removeResult = AcmInterop.acmDriverRemove(localDriver.driverId, 0); // gets stored as a driver Id
            NativeMethods.FreeLibrary(localDriver.localDllHandle);
            MmException.Try(removeResult, "acmDriverRemove");
        }

        /// <summary>
        /// Displays a format selection dialog and returns the selected format and description.
        /// </summary>
        /// <param name="ownerWindowHandle">The handle to the owner window.</param>
        /// <param name="windowTitle">The title of the dialog window.</param>
        /// <param name="enumFlags">The enumeration flags for the format selection.</param>
        /// <param name="enumFormat">The format to be enumerated.</param>
        /// <param name="selectedFormat">When this method returns, contains the selected wave format.</param>
        /// <param name="selectedFormatDescription">When this method returns, contains the description of the selected wave format.</param>
        /// <param name="selectedFormatTagDescription">When this method returns, contains the tag description of the selected wave format.</param>
        /// <returns>True if the format selection was successful; otherwise, false.</returns>
        /// <remarks>
        /// This method displays a format selection dialog using the given parameters and returns the selected format and its description.
        /// If an error occurs during the format selection, a <see cref="MmException"/> is thrown with the appropriate error message.
        /// </remarks>
        public static bool ShowFormatChooseDialog(
            IntPtr ownerWindowHandle,
            string windowTitle,
            AcmFormatEnumFlags enumFlags,
            WaveFormat enumFormat,
            out WaveFormat selectedFormat,
            out string selectedFormatDescription,
            out string selectedFormatTagDescription)
        {
            AcmFormatChoose formatChoose = new AcmFormatChoose();
            formatChoose.structureSize = Marshal.SizeOf(formatChoose);
            formatChoose.styleFlags = AcmFormatChooseStyleFlags.None;
            formatChoose.ownerWindowHandle = ownerWindowHandle;
            int maxFormatSize = 200; // guess
            formatChoose.selectedWaveFormatPointer = Marshal.AllocHGlobal(maxFormatSize);
            formatChoose.selectedWaveFormatByteSize = maxFormatSize;
            formatChoose.title = windowTitle;
            formatChoose.name = null;
            formatChoose.formatEnumFlags = enumFlags;//AcmFormatEnumFlags.None;
            formatChoose.waveFormatEnumPointer = IntPtr.Zero;
            if (enumFormat != null)
            {
                IntPtr enumPointer = Marshal.AllocHGlobal(Marshal.SizeOf(enumFormat));
                Marshal.StructureToPtr(enumFormat,enumPointer,false);
                formatChoose.waveFormatEnumPointer = enumPointer;
            }
            formatChoose.instanceHandle = IntPtr.Zero;
            formatChoose.templateName = null;

            MmResult result = AcmInterop.acmFormatChoose(ref formatChoose);
            selectedFormat = null;
            selectedFormatDescription = null;
            selectedFormatTagDescription = null;
            if (result == MmResult.NoError)
            {
                selectedFormat = WaveFormat.MarshalFromPtr(formatChoose.selectedWaveFormatPointer);
                selectedFormatDescription = formatChoose.formatDescription;
                selectedFormatTagDescription = formatChoose.formatTagDescription;
            }            
            
            Marshal.FreeHGlobal(formatChoose.waveFormatEnumPointer);
            Marshal.FreeHGlobal(formatChoose.selectedWaveFormatPointer);
            if(result != MmResult.AcmCancelled && result != MmResult.NoError)
            {                
                throw new MmException(result, "acmFormatChoose");
            }
            return result == MmResult.NoError;
            
        }

        /// <summary>
        /// Gets the maximum size needed to store a WaveFormat for ACM interop functions
        /// </summary>
        public int MaxFormatSize
        {
            get
            {
                MmException.Try(AcmInterop.acmMetrics(driverHandle, AcmMetrics.MaxSizeFormat, out int maxFormatSize), "acmMetrics");
                return maxFormatSize;
            }
        }

        /// <summary>
        /// Finds an AcmDriver by its short name.
        /// </summary>
        /// <param name="shortName">The short name of the AcmDriver to find.</param>
        /// <returns>The AcmDriver with the specified <paramref name="shortName"/>, or null if not found.</returns>
        public static AcmDriver FindByShortName(string shortName)
        {
            foreach (AcmDriver driver in AcmDriver.EnumerateAcmDrivers())
            {
                if (driver.ShortName == shortName)
                {
                    return driver;
                }
            }
            return null;
        }

        /// <summary>
        /// Enumerates the available audio compression managers (ACM) drivers and returns a collection of <see cref="AcmDriver"/>.
        /// </summary>
        /// <returns>A collection of <see cref="AcmDriver"/> representing the available ACM drivers.</returns>
        /// <remarks>
        /// This method internally calls the acmDriverEnum function to enumerate the available ACM drivers.
        /// It populates the <see cref="drivers"/> collection with the available drivers and returns the collection.
        /// </remarks>
        /// <exception cref="MmException">Thrown when an error occurs during the acmDriverEnum function call.</exception>
        public static IEnumerable<AcmDriver> EnumerateAcmDrivers()
        {
            drivers = new List<AcmDriver>();
            MmException.Try(AcmInterop.acmDriverEnum(new AcmInterop.AcmDriverEnumCallback(DriverEnumCallback), IntPtr.Zero, 0), "acmDriverEnum");
            return drivers;
        }

        /// <summary>
        /// Callback function for enumerating ACM drivers.
        /// </summary>
        /// <param name="hAcmDriver">Handle to an ACM driver.</param>
        /// <param name="dwInstance">Application-defined value specified in the acmDriverEnum function.</param>
        /// <param name="flags">Support flags for the ACM driver.</param>
        /// <returns>True if the ACM driver details were successfully added to the list; otherwise, false.</returns>
        /// <remarks>
        /// This callback function is used for enumerating ACM drivers. It adds the details of the ACM driver specified by the handle <paramref name="hAcmDriver"/> to the list of drivers.
        /// The application-defined value <paramref name="dwInstance"/> is used for additional context during enumeration.
        /// The support flags <paramref name="flags"/> provide information about the capabilities of the ACM driver.
        /// </remarks>
        private static bool DriverEnumCallback(IntPtr hAcmDriver, IntPtr dwInstance, AcmDriverDetailsSupportFlags flags)
        {
            drivers.Add(new AcmDriver(hAcmDriver));
            return true;
        }

        /// <summary>
        /// Creates a new ACM Driver object
        /// </summary>
        /// <param name="hAcmDriver">Driver handle</param>
        private AcmDriver(IntPtr hAcmDriver)
        {
            driverId = hAcmDriver;
            details = new AcmDriverDetails();
            details.structureSize = Marshal.SizeOf(details);
            MmException.Try(AcmInterop.acmDriverDetails(hAcmDriver, ref details, 0), "acmDriverDetails");
        }

        /// <summary>
        /// The short name of this driver
        /// </summary>
        public string ShortName
        {
            get
            {
                return details.shortName;
            }
        }

        /// <summary>
        /// The full name of this driver
        /// </summary>
        public string LongName
        {
            get
            {
                return details.longName;
            }
        }

        /// <summary>
        /// The driver ID
        /// </summary>
        public IntPtr DriverId
        {
            get
            {
                return driverId;
            }
        }

        /// <summary>
        /// Returns the long name of the object as a string.
        /// </summary>
        /// <returns>The long name of the object as a string.</returns>
        public override string ToString()
        {
            return LongName;
        }

        /// <summary>
        /// The list of FormatTags for this ACM Driver
        /// </summary>
        public IEnumerable<AcmFormatTag> FormatTags
        {
            get
            {
                if (formatTags == null)
                {
                    if (driverHandle == IntPtr.Zero)
                    {
                        throw new InvalidOperationException("Driver must be opened first");
                    }
                    formatTags = new List<AcmFormatTag>();
                    AcmFormatTagDetails formatTagDetails = new AcmFormatTagDetails();
                    formatTagDetails.structureSize = Marshal.SizeOf(formatTagDetails);
                    MmException.Try(AcmInterop.acmFormatTagEnum(this.driverHandle, ref formatTagDetails, AcmFormatTagEnumCallback, IntPtr.Zero, 0), "acmFormatTagEnum");
                }
                return formatTags;
            }
        }

        /// <summary>
        /// Retrieves a collection of audio formats associated with the specified format tag.
        /// </summary>
        /// <param name="formatTag">The format tag to filter the audio formats.</param>
        /// <exception cref="InvalidOperationException">Thrown when the driver has not been opened prior to calling this method.</exception>
        /// <returns>A collection of <see cref="AcmFormat"/> objects representing the audio formats associated with the specified format tag.</returns>
        /// <remarks>
        /// This method retrieves a collection of audio formats associated with the specified format tag from the driver.
        /// It allocates memory for a wave format and uses the <see cref="AcmInterop.acmFormatEnum"/> method to enumerate the audio formats.
        /// The retrieved audio formats are stored in the <see cref="tempFormatsList"/> collection and returned.
        /// </remarks>
        public IEnumerable<AcmFormat> GetFormats(AcmFormatTag formatTag)
        {
            if (driverHandle == IntPtr.Zero)
            {
                throw new InvalidOperationException("Driver must be opened first");
            }
            tempFormatsList = new List<AcmFormat>();
            var formatDetails = new AcmFormatDetails();
            formatDetails.structSize = Marshal.SizeOf(formatDetails);
            // need to make sure we have enough space for a waveFormat. formatTag.FormatSize isn't reliable, 
            // and some codecs MaxFormatSize isn't either
            formatDetails.waveFormatByteSize = 1024;
            formatDetails.waveFormatPointer = Marshal.AllocHGlobal(formatDetails.waveFormatByteSize);
            formatDetails.formatTag = (int)formatTag.FormatTag; // (int)WaveFormatEncoding.Unknown
            var result = AcmInterop.acmFormatEnum(driverHandle, 
                ref formatDetails, AcmFormatEnumCallback, IntPtr.Zero, 
                AcmFormatEnumFlags.None);
            Marshal.FreeHGlobal(formatDetails.waveFormatPointer);
            MmException.Try(result,"acmFormatEnum");
            return tempFormatsList;
        }

        /// <summary>
        /// Opens the audio driver if it is not already open.
        /// </summary>
        /// <exception cref="MmException">Thrown when an error occurs while opening the audio driver.</exception>
        /// <remarks>
        /// This method checks if the <paramref name="driverHandle"/> is equal to <see cref="IntPtr.Zero"/> and then attempts to open the driver using the <see cref="AcmInterop.acmDriverOpen"/> method.
        /// If the driver is already open, this method does nothing. If an error occurs during the opening process, a <see cref="MmException"/> is thrown with the message "acmDriverOpen".
        /// </remarks>
        public void Open()
        {
            if (driverHandle == IntPtr.Zero)
            {
                MmException.Try(AcmInterop.acmDriverOpen(out driverHandle, DriverId, 0), "acmDriverOpen");
            }
        }

        /// <summary>
        /// Closes the driver handle if it is not already closed.
        /// </summary>
        /// <remarks>
        /// This method checks if the <paramref name="driverHandle"/> is not equal to <see cref="IntPtr.Zero"/>,
        /// and if so, it calls <see cref="AcmInterop.acmDriverClose"/> to close the driver handle with flags set to 0.
        /// After closing the driver handle, it sets the <paramref name="driverHandle"/> to <see cref="IntPtr.Zero"/>.
        /// </remarks>
        /// <exception cref="MmException">Thrown when an error occurs during the driver close operation.</exception>
        public void Close()
        {
            if(driverHandle != IntPtr.Zero)
            {
                MmException.Try(AcmInterop.acmDriverClose(driverHandle, 0),"acmDriverClose");
                driverHandle = IntPtr.Zero;
            }
        }

        /// <summary>
        /// Callback function for enumerating ACM format tags. Adds the details of the format tag to the collection and returns true.
        /// </summary>
        /// <param name="hAcmDriverId">Handle to the ACM driver identifier.</param>
        /// <param name="formatTagDetails">Reference to the details of the format tag being enumerated.</param>
        /// <param name="dwInstance">Pointer to a user-defined instance data specified during the enumeration.</param>
        /// <param name="flags">Flags indicating the driver support details.</param>
        /// <returns>True if the format tag details are successfully added to the collection; otherwise, false.</returns>
        /// <remarks>
        /// This callback function is used for enumerating ACM format tags. It adds the details of the format tag to the collection and returns true upon successful addition.
        /// </remarks>
        private bool AcmFormatTagEnumCallback(IntPtr hAcmDriverId, ref AcmFormatTagDetails formatTagDetails, IntPtr dwInstance, AcmDriverDetailsSupportFlags flags)
        {
            formatTags.Add(new AcmFormatTag(formatTagDetails));
            return true;
        }

        /// <summary>
        /// Callback function for enumerating ACM format details.
        /// </summary>
        /// <param name="hAcmDriverId">Handle to the ACM driver identifier.</param>
        /// <param name="formatDetails">Reference to the ACM format details.</param>
        /// <param name="dwInstance">Pointer to application-defined data.</param>
        /// <param name="flags">Flags indicating the support details of the ACM driver.</param>
        /// <returns>True if the ACM format details are successfully added to the temporary formats list; otherwise, false.</returns>
        /// <remarks>
        /// This callback function is used for enumerating ACM format details. It adds the ACM format details to the temporary formats list and returns a boolean value indicating the success of the operation.
        /// </remarks>
        private bool AcmFormatEnumCallback(IntPtr hAcmDriverId, ref AcmFormatDetails formatDetails, IntPtr dwInstance, AcmDriverDetailsSupportFlags flags)
        {
            tempFormatsList.Add(new AcmFormat(formatDetails));
            return true;
        }

        /// <summary>
        /// Disposes of the resources used by the current instance.
        /// </summary>
        /// <remarks>
        /// This method checks if the <see cref="driverHandle"/> is not equal to <see cref="IntPtr.Zero"/>, and if so, it calls the <see cref="Close"/> method and suppresses the finalization of the current instance by the garbage collector.
        /// </remarks>
        public void Dispose()
        {
            if (driverHandle != IntPtr.Zero)
            {
                Close();
                GC.SuppressFinalize(this);
            }
        }

        #endregion
    }

}
