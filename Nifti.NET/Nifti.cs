
using System;

namespace Nifti.NET
{
    /// <summary>
    /// The Dynamically typed version of a nifti object. Can be used for simple and direct access from files.
    /// </summary>
    public class Nifti
    {
        /// <summary>
        /// Private storage for header object.
        /// </summary>
        private NiftiHeader _header;

        /// <summary>
        /// The NIfTI file header object.
        /// </summary>
        public NiftiHeader Header
        {
            get
            {
                return _header;
            }
            set
            {
                // Set dimensions.
                Dimensions = new int[value.dim[0]];
                Stride = new int[value.dim[0]];

                for (int i = 0; i < Dimensions.Length; ++i)
                {
                    Dimensions[i] = value.dim[i + 1];

                    // Work out stride...
                    var stride = 1;
                    if (i > 0) for (int j = i - 1; j >= 0; --j) stride *= Dimensions[j];
                    Stride[i] = stride;
                }

                _header = value;
            }
        }

        // Volume interface implementation.
        /// <summary>
        /// The stride for the indexed dimension. This allows direct access to the data.
        /// </summary>
        public int[] Stride { get; private set; }
        /// <summary>
        /// The length of each dimension.
        /// </summary>
        public int[] Dimensions { get; set; }
        /// <summary>
        /// The underlying data structure as an array for fast linear processing.
        /// </summary>
        public dynamic Data { get; set; }
        /// <summary>
        /// Allows multi-dimensional access to the data.
        /// These coordinates correspond with the i, j, k system in the NiftiHeader, meaning the stride will increase from i to k (and onwards).
        /// </summary>
        /// <param name="idx"></param>
        /// <returns></returns>
        public dynamic this[params int[] idx]
        {
            get { return Data[DataIndexFor(idx)]; }
            set { Data[DataIndexFor(idx)] = value; }
        }

        /// <summary>
        /// Returns the linear index for a given multi-dimensional index.
        /// </summary>
        /// <param name="idx"></param>
        /// <returns></returns>
        protected int DataIndexFor(params int[] idx)
        {
            // Because this is going to be in a lot of inner loops, we'll 
            // use if statements to catch the common cases (I've heard it makes for easier optimizing in the compiler)...
            if (idx.Length == 3) return idx[0] * Stride[0] + idx[1] * Stride[1] + idx[2] * Stride[2];
            else if (idx.Length == 2) return idx[0] * Stride[0] + idx[1] * Stride[1];

            // Now we'll handle the general case...
            int index = 0;

            for (int i = 0; i < idx.Length; ++i)
            {
                index += idx[i] * Stride[i];
            }
            return index;
        }

        /// <summary>
        /// Will return true if the underlying data is of the given type, otherwise false.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public bool IsType(Type type)
        {
            if (type == null) return false;
            if (type == typeof(byte) && Header.datatype == NiftiHeader.NIFTI_TYPE_INT8) return true;
            if (type == typeof(short) && Header.datatype == NiftiHeader.NIFTI_TYPE_INT16) return true;
            if (type == typeof(int) && Header.datatype == NiftiHeader.NIFTI_TYPE_INT32) return true;
            if (type == typeof(long) && Header.datatype == NiftiHeader.NIFTI_TYPE_INT64) return true;

            if (type == typeof(byte) && Header.datatype == NiftiHeader.NIFTI_TYPE_UINT8) return true;
            if (type == typeof(ushort) && Header.datatype == NiftiHeader.NIFTI_TYPE_UINT16) return true;
            if (type == typeof(uint) && Header.datatype == NiftiHeader.NIFTI_TYPE_UINT32) return true;
            if (type == typeof(ulong) && Header.datatype == NiftiHeader.NIFTI_TYPE_UINT64) return true;

            if (type == typeof(float) && Header.datatype == NiftiHeader.NIFTI_TYPE_FLOAT32) return true;
            if (type == typeof(double) && Header.datatype == NiftiHeader.NIFTI_TYPE_FLOAT64) return true;

            return false;
        }

        /// <summary>
        /// Will create a shallow copy of this NIfTI object, which is of the given type.
        /// If the underlying data is not of type T, this method will throw a format exception,
        /// so it's best to use IsType to check the type first.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public Nifti<T> AsType<T>()
        {
            if (!IsType(typeof(T))) throw new FormatException("The Nifti is not of type " + typeof(T));

            return new Nifti<T>
            {
                Header = Header,
                Data = (T[])Data
            };
        }

        /// <summary>
        /// Creates a new Nifti object from a data array with specified dimensions.
        /// </summary>
        /// <typeparam name="T">The data type of the array</typeparam>
        /// <param name="data">The data array</param>
        /// <param name="dimensions">The dimensions of the data (e.g., [width, height, depth])</param>
        /// <param name="pixelDimensions">The pixel dimensions/spacing (optional)</param>
        /// <param name="description">Description of the data (optional)</param>
        /// <returns>A new Nifti object</returns>
        public static Nifti<T> CreateFromData<T>(T[] data, int[] dimensions, float[] pixelDimensions = null, string description = null)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (dimensions == null || dimensions.Length == 0) throw new ArgumentNullException(nameof(dimensions));
            if (dimensions.Length > 7) throw new ArgumentException("Maximum of 7 dimensions supported", nameof(dimensions));

            // Validate data length matches dimensions
            long expectedLength = 1;
            for (int i = 0; i < dimensions.Length; i++)
            {
                if (dimensions[i] <= 0) throw new ArgumentException($"Dimension {i} must be positive", nameof(dimensions));
                expectedLength *= dimensions[i];
            }
            if (data.Length != expectedLength) throw new ArgumentException($"Data length {data.Length} does not match expected length {expectedLength} from dimensions");

            var nifti = new Nifti<T>();
            var header = new NiftiHeader();

            // Set dimensions
            header.dim = new short[8];
            header.dim[0] = (short)dimensions.Length;
            for (int i = 0; i < dimensions.Length; i++)
            {
                header.dim[i + 1] = (short)dimensions[i];
            }
            for (int i = dimensions.Length + 1; i < 8; i++)
            {
                header.dim[i] = 1;
            }

            // Set pixel dimensions
            header.pixdim = new float[8];
            if (pixelDimensions != null)
            {
                for (int i = 0; i < Math.Min(pixelDimensions.Length, 7); i++)
                {
                    header.pixdim[i + 1] = pixelDimensions[i];
                }
            }
            else
            {
                for (int i = 1; i < 8; i++)
                {
                    header.pixdim[i] = 1.0f;
                }
            }

            // Set data type based on T
            header.datatype = GetDataTypeForType<T>();
            header.bitpix = GetBitsPerPixelForType<T>();

            // Set magic number for single file format
            header.magic = new byte[] { 0x6E, 0x2B, 0x31, 0x00 }; // "n+1\0"
            header.vox_offset = 352.0f;

            // Set description if provided
            if (!string.IsNullOrEmpty(description))
            {
                var descBytes = System.Text.Encoding.ASCII.GetBytes(description);
                header.descrip = new byte[80];
                Array.Copy(descBytes, header.descrip, Math.Min(descBytes.Length, 79));
            }

            // Set header size
            header.sizeof_hdr = NiftiHeader.EXPECTED_SIZE_OF_HDR;

            nifti.Header = header;
            nifti.Data = data;

            return nifti;
        }

        /// <summary>
        /// Creates a new Nifti object from a data array with specified dimensions.
        /// </summary>
        /// <param name="data">The data array</param>
        /// <param name="dimensions">The dimensions of the data (e.g., [width, height, depth])</param>
        /// <param name="pixelDimensions">The pixel dimensions/spacing (optional)</param>
        /// <param name="description">Description of the data (optional)</param>
        /// <returns>A new Nifti object</returns>
        public static Nifti CreateFromData(Array data, int[] dimensions, float[] pixelDimensions = null, string description = null)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            
            var elementType = data.GetType().GetElementType();
            
            if (elementType == typeof(float))
                return CreateFromData((float[])data, dimensions, pixelDimensions, description);
            else if (elementType == typeof(double))
                return CreateFromData((double[])data, dimensions, pixelDimensions, description);
            else if (elementType == typeof(int))
                return CreateFromData((int[])data, dimensions, pixelDimensions, description);
            else if (elementType == typeof(short))
                return CreateFromData((short[])data, dimensions, pixelDimensions, description);
            else if (elementType == typeof(byte))
                return CreateFromData((byte[])data, dimensions, pixelDimensions, description);
            else if (elementType == typeof(ushort))
                return CreateFromData((ushort[])data, dimensions, pixelDimensions, description);
            else if (elementType == typeof(uint))
                return CreateFromData((uint[])data, dimensions, pixelDimensions, description);
            else if (elementType == typeof(long))
                return CreateFromData((long[])data, dimensions, pixelDimensions, description);
            else
                throw new NotSupportedException($"Data type {elementType} is not supported");
        }

        private static short GetDataTypeForType<T>()
        {
            var type = typeof(T);
            if (type == typeof(byte)) return NiftiHeader.DT_UINT8;
            if (type == typeof(short)) return NiftiHeader.DT_INT16;
            if (type == typeof(int)) return NiftiHeader.DT_INT32;
            if (type == typeof(long)) return NiftiHeader.DT_COMPLEX;
            if (type == typeof(ushort)) return NiftiHeader.DT_UINT16;
            if (type == typeof(uint)) return NiftiHeader.DT_UINT32;
            if (type == typeof(float)) return NiftiHeader.DT_FLOAT32;
            if (type == typeof(double)) return NiftiHeader.DT_DOUBLE;
            throw new NotSupportedException($"Type {type} is not supported for NIfTI data");
        }

        private static short GetBitsPerPixelForType<T>()
        {
            var type = typeof(T);
            if (type == typeof(byte)) return 8;
            if (type == typeof(short)) return 16;
            if (type == typeof(int)) return 32;
            if (type == typeof(long)) return 64;
            if (type == typeof(ushort)) return 16;
            if (type == typeof(uint)) return 32;
            if (type == typeof(float)) return 32;
            if (type == typeof(double)) return 64;
            throw new NotSupportedException($"Type {type} is not supported for NIfTI data");
        }
    }

    /// <summary>
    /// A typed version of the Nifti data.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Nifti<T> : Nifti
    {
        public new T[] Data { get { return ((Nifti)this).Data; } set { ((Nifti)this).Data = value; } }

        public new T this[params int[] idx]
        {
            get { return Data[DataIndexFor(idx)]; }
            set { Data[DataIndexFor(idx)] = value; }
        }
    }
}
