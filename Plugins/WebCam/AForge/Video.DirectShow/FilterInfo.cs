// AForge Direct Show Library
// AForge.NET framework
//
// Copyright © Andrew Kirillov, 2008
// andrew.kirillov@gmail.com
//

namespace AForge.Video.DirectShow
{
    using System;
    using System.Runtime.InteropServices;
    using System.Runtime.InteropServices.ComTypes;
    using AForge.Video.DirectShow.Internals;

    /// <summary>
    /// DirectShow filter information.
    /// </summary>
    /// 
    public class FilterInfo : IComparable
    {
        /// <summary>
        /// Filter name.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Filters's moniker string.
        /// </summary>
        /// 
        public string MonikerString { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="FilterInfo"/> class.
        /// </summary>
        /// 
        /// <param name="monikerString">Filters's moniker string.</param>
        /// 
        public FilterInfo( string monikerString )
        {
            MonikerString = monikerString;
            Name = GetName( monikerString );
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FilterInfo"/> class.
        /// </summary>
        /// 
        /// <param name="moniker">Filter's moniker object.</param>
        /// 
        internal FilterInfo( IMoniker moniker )
        {
            MonikerString = GetMonikerString( moniker );
            Name = GetName( moniker );
        }

        /// <summary>
        /// Compares the current FilterInfo object with another object and returns an integer that indicates whether the current object precedes, follows, or occurs in the same position in the sort order as the other object.
        /// </summary>
        /// <param name="value">The object to compare with this instance.</param>
        /// <returns>A signed integer that indicates the relative position of this instance and <paramref name="value"/> in the sort order.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="value"/> is not a FilterInfo object.</exception>
        public int CompareTo( object value )
        {
            FilterInfo f = (FilterInfo) value;

            if ( f == null )
                return 1;

            return ( this.Name.CompareTo( f.Name ) );
        }

        /// <summary>
        /// Creates a filter object based on the provided filter moniker.
        /// </summary>
        /// <param name="filterMoniker">The moniker string representing the filter.</param>
        /// <returns>The filter object created based on the provided moniker.</returns>
        /// <remarks>
        /// This method creates a bind context and attempts to convert the filter moniker string to a moniker.
        /// It then retrieves the device base filter using the moniker and returns the filter object.
        /// </remarks>
        public static object CreateFilter( string filterMoniker )
        {
            // filter's object
            object filterObject = null;
            // bind context and moniker objects
            IBindCtx bindCtx = null;
            IMoniker moniker = null;

            int n = 0;

            // create bind context
            if ( Win32.CreateBindCtx( 0, out bindCtx ) == 0 )
            {
                // convert moniker`s string to a moniker
                if ( Win32.MkParseDisplayName( bindCtx, filterMoniker, ref n, out moniker ) == 0 )
                {
                    // get device base filter
                    Guid filterId = typeof( IBaseFilter ).GUID;
                    moniker.BindToObject( null, null, ref filterId, out filterObject );

                    Marshal.ReleaseComObject( moniker );
                }
                Marshal.ReleaseComObject( bindCtx );
            }
            return filterObject;
        }

        /// <summary>
        /// Retrieves the display name of the specified moniker.
        /// </summary>
        /// <param name="moniker">The moniker for which the display name is to be retrieved.</param>
        /// <returns>The display name of the specified <paramref name="moniker"/>.</returns>
        private string GetMonikerString( IMoniker moniker )
        {
            string str;
            moniker.GetDisplayName( null, null, out str );
            return str;
        }

        /// <summary>
        /// Retrieves the name associated with the given moniker string.
        /// </summary>
        /// <param name="monikerString">The moniker string to retrieve the name for.</param>
        /// <returns>The name associated with the given moniker string.</returns>
        /// <exception cref="System.Runtime.InteropServices.COMException">Thrown when there is an error in creating the bind context or converting the moniker's string to a moniker.</exception>
        private string GetName( IMoniker moniker )
        {
            Object bagObj = null;
            IPropertyBag bag = null;

            try
            {
                Guid bagId = typeof( IPropertyBag ).GUID;
                // get property bag of the moniker
                moniker.BindToStorage( null, null, ref bagId, out bagObj );
                bag = (IPropertyBag) bagObj;

                // read FriendlyName
                object val = "";
                int hr = bag.Read( "FriendlyName", ref val, IntPtr.Zero );
                if ( hr != 0 )
                    Marshal.ThrowExceptionForHR( hr );

                // get it as string
                string ret = (string) val;
                if ( ( ret == null ) || ( ret.Length < 1 ) )
                    throw new ApplicationException( );

                return ret;
            }
            catch ( Exception )
            {
                return "";
            }
            finally
            {
                // release all COM objects
                bag = null;
                if ( bagObj != null )
                {
                    Marshal.ReleaseComObject( bagObj );
                    bagObj = null;
                }
            }
        }

        //
        // Get filter name represented by the moniker string
        //
        private string GetName( string monikerString )
        {
            IBindCtx bindCtx = null;
            IMoniker moniker = null;
            String name = "";
            int n = 0;

            // create bind context
            if ( Win32.CreateBindCtx( 0, out bindCtx ) == 0 )
            {
                // convert moniker`s string to a moniker
                if ( Win32.MkParseDisplayName( bindCtx, monikerString, ref n, out moniker ) == 0 )
                {
                    // get device name
                    name = GetName( moniker );

                    Marshal.ReleaseComObject( moniker );
                    moniker = null;
                }
                Marshal.ReleaseComObject( bindCtx );
                bindCtx = null;
            }
            return name;
        }
    }
}
