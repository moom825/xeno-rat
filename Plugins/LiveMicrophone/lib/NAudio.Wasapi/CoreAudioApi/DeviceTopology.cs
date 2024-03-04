using NAudio.CoreAudioApi.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace NAudio.CoreAudioApi
{
    /// <summary>
    /// Windows CoreAudio DeviceTopology
    /// </summary>
    public class DeviceTopology
    {
        private readonly IDeviceTopology deviceTopologyInterface;

        internal DeviceTopology(IDeviceTopology deviceTopology)
        {
            deviceTopologyInterface = deviceTopology;
        }

        /// <summary>
        /// Retrieves the number of connections associated with this device-topology object
        /// </summary>
        public uint ConnectorCount
        {
            get
            {
                deviceTopologyInterface.GetConnectorCount(out var count);
                return count;
            }
        }

        /// <summary>
        /// Gets the connector at the specified index.
        /// </summary>
        /// <param name="index">The index of the connector to retrieve.</param>
        /// <returns>A new instance of the Connector class representing the connector at the specified index.</returns>
        /// <remarks>
        /// This method retrieves the connector at the specified index from the device topology interface and creates a new instance of the Connector class to represent it.
        /// </remarks>
        public Connector GetConnector(uint index)
        {
            deviceTopologyInterface.GetConnector(index, out var connectorInterface);
            return new Connector(connectorInterface);
        }

        /// <summary>
        /// Retrieves the device id of the device represented by this device-topology object
        /// </summary>
        public string DeviceId
        {
            get
            {
                deviceTopologyInterface.GetDeviceId(out var result);
                return result;
            }
        }

    }
}
