using System;
using NUnit.Framework;
using Nifti.NET;

namespace Nifti.NET.Tests
{
    [TestFixture]
    public class CreateFromDataTests
    {
        [Test]
        public void CreateFromData_Float_3D_ShouldCreateValidNiftiObject()
        {
            // Arrange
            var data = new float[2 * 3 * 4];
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = i + 1.0f;
            }
            var dimensions = new int[] { 2, 3, 4 };
            var pixelDimensions = new float[] { 1.0f, 1.0f, 1.0f };

            // Act
            var nifti = Nifti.CreateFromData(data, dimensions, pixelDimensions, "Test 3D data");

            // Assert
            Assert.IsNotNull(nifti);
            Assert.AreEqual(3, nifti.Header.dim[0]); // 3 dimensions
            Assert.AreEqual(2, nifti.Header.dim[1]);
            Assert.AreEqual(3, nifti.Header.dim[2]);
            Assert.AreEqual(4, nifti.Header.dim[3]);
            Assert.AreEqual(NiftiHeader.DT_FLOAT32, nifti.Header.datatype);
            Assert.AreEqual(32, nifti.Header.bitpix);
            Assert.AreEqual(data.Length, ((float[])nifti.Data).Length);
            Assert.AreEqual(1.0f, ((float[])nifti.Data)[0]);
            Assert.AreEqual(24.0f, ((float[])nifti.Data)[23]);
        }

        [Test]
        public void CreateFromData_TypedFloat_ShouldCreateTypedNiftiObject()
        {
            // Arrange
            var data = new float[6] { 1.0f, 2.0f, 3.0f, 4.0f, 5.0f, 6.0f };
            var dimensions = new int[] { 2, 3 };

            // Act
            var nifti = Nifti.CreateFromData<float>(data, dimensions);

            // Assert
            Assert.IsNotNull(nifti);
            Assert.IsInstanceOfType(nifti, typeof(Nifti<float>));
            Assert.AreEqual(2, nifti.Header.dim[0]); // 2 dimensions
            Assert.AreEqual(2, nifti.Header.dim[1]);
            Assert.AreEqual(3, nifti.Header.dim[2]);
            Assert.AreEqual(6, nifti.Data.Length);
            Assert.AreEqual(1.0f, nifti.Data[0]);
            Assert.AreEqual(6.0f, nifti.Data[5]);
        }

        [Test]
        public void CreateFromData_Int_ShouldCreateValidNiftiObject()
        {
            // Arrange
            var data = new int[4] { 10, 20, 30, 40 };
            var dimensions = new int[] { 2, 2 };

            // Act
            var nifti = Nifti.CreateFromData(data, dimensions);

            // Assert
            Assert.IsNotNull(nifti);
            Assert.AreEqual(NiftiHeader.DT_INT32, nifti.Header.datatype);
            Assert.AreEqual(32, nifti.Header.bitpix);
            Assert.AreEqual(4, ((int[])nifti.Data).Length);
            Assert.AreEqual(10, ((int[])nifti.Data)[0]);
        }

        [Test]
        public void CreateFromData_Byte_ShouldCreateValidNiftiObject()
        {
            // Arrange
            var data = new byte[8] { 1, 2, 3, 4, 5, 6, 7, 8 };
            var dimensions = new int[] { 2, 2, 2 };

            // Act
            var nifti = Nifti.CreateFromData(data, dimensions);

            // Assert
            Assert.IsNotNull(nifti);
            Assert.AreEqual(NiftiHeader.DT_UINT8, nifti.Header.datatype);
            Assert.AreEqual(8, nifti.Header.bitpix);
            Assert.AreEqual(8, ((byte[])nifti.Data).Length);
        }

        [Test]
        public void CreateFromData_WithDescription_ShouldSetDescription()
        {
            // Arrange
            var data = new float[2] { 1.0f, 2.0f };
            var dimensions = new int[] { 2 };
            var description = "Test description";

            // Act
            var nifti = Nifti.CreateFromData(data, dimensions, description: description);

            // Assert
            var descString = System.Text.Encoding.ASCII.GetString(nifti.Header.descrip).TrimEnd('\0');
            Assert.AreEqual(description, descString);
        }

        [Test]
        public void CreateFromData_WithPixelDimensions_ShouldSetPixelDimensions()
        {
            // Arrange
            var data = new float[8] { 1, 2, 3, 4, 5, 6, 7, 8 };
            var dimensions = new int[] { 2, 2, 2 };
            var pixelDimensions = new float[] { 0.5f, 1.0f, 2.0f };

            // Act
            var nifti = Nifti.CreateFromData(data, dimensions, pixelDimensions);

            // Assert
            Assert.AreEqual(0.5f, nifti.Header.pixdim[1]);
            Assert.AreEqual(1.0f, nifti.Header.pixdim[2]);
            Assert.AreEqual(2.0f, nifti.Header.pixdim[3]);
        }

        [Test]
        public void CreateFromData_NullData_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => Nifti.CreateFromData<float>(null, new int[] { 1 }));
        }

        [Test]
        public void CreateFromData_NullDimensions_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => Nifti.CreateFromData(new float[1], null));
        }

        [Test]
        public void CreateFromData_EmptyDimensions_ShouldThrowArgumentException()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => Nifti.CreateFromData(new float[1], new int[0]));
        }

        [Test]
        public void CreateFromData_TooManyDimensions_ShouldThrowArgumentException()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => Nifti.CreateFromData(new float[1], new int[8] { 1, 1, 1, 1, 1, 1, 1, 1 }));
        }

        [Test]
        public void CreateFromData_InvalidDimension_ShouldThrowArgumentException()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => Nifti.CreateFromData(new float[1], new int[] { 0 }));
        }

        [Test]
        public void CreateFromData_MismatchedDataLength_ShouldThrowArgumentException()
        {
            // Arrange - data has 4 elements but dimensions specify 6 elements (2*3)
            var data = new float[4] { 1, 2, 3, 4 };
            var dimensions = new int[] { 2, 3 };

            // Act & Assert
            Assert.Throws<ArgumentException>(() => Nifti.CreateFromData(data, dimensions));
        }

        [Test]
        public void CreateFromData_4D_ShouldCreateValidNiftiObject()
        {
            // Arrange
            var data = new float[2 * 3 * 4 * 5];
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = i;
            }
            var dimensions = new int[] { 2, 3, 4, 5 };

            // Act
            var nifti = Nifti.CreateFromData(data, dimensions);

            // Assert
            Assert.AreEqual(4, nifti.Header.dim[0]); // 4 dimensions
            Assert.AreEqual(2, nifti.Header.dim[1]);
            Assert.AreEqual(3, nifti.Header.dim[2]);
            Assert.AreEqual(4, nifti.Header.dim[3]);
            Assert.AreEqual(5, nifti.Header.dim[4]);
            Assert.AreEqual(120, ((float[])nifti.Data).Length);
        }

        [Test]
        public void CreateFromData_ShouldSetCorrectMagicNumber()
        {
            // Arrange
            var data = new float[1] { 1.0f };
            var dimensions = new int[] { 1 };

            // Act
            var nifti = Nifti.CreateFromData(data, dimensions);

            // Assert
            Assert.AreEqual(0x6E, nifti.Header.magic[0]); // 'n'
            Assert.AreEqual(0x2B, nifti.Header.magic[1]); // '+'
            Assert.AreEqual(0x31, nifti.Header.magic[2]); // '1'
            Assert.AreEqual(0x00, nifti.Header.magic[3]); // '\0'
        }

        [Test]
        public void CreateFromData_ShouldSetCorrectHeaderSize()
        {
            // Arrange
            var data = new float[1] { 1.0f };
            var dimensions = new int[] { 1 };

            // Act
            var nifti = Nifti.CreateFromData(data, dimensions);

            // Assert
            Assert.AreEqual(348, nifti.Header.sizeof_hdr);
        }

        [Test]
        public void CreateFromData_AccessViaIndexer_ShouldWork()
        {
            // Arrange
            var data = new float[6] { 1, 2, 3, 4, 5, 6 };
            var dimensions = new int[] { 2, 3 };

            // Act
            var nifti = Nifti.CreateFromData(data, dimensions);

            // Assert
            Assert.AreEqual(1.0f, nifti[0, 0]);
            Assert.AreEqual(2.0f, nifti[1, 0]);
            Assert.AreEqual(3.0f, nifti[0, 1]);
            Assert.AreEqual(6.0f, nifti[1, 2]);
        }
    }
}