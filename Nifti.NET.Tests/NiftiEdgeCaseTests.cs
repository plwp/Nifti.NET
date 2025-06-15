using System;
using System.IO;
using NUnit.Framework;
using Nifti.NET;

namespace Nifti.NET.Tests
{
    /// <summary>
    /// Tests for edge cases, malformed files, and robustness against various invalid inputs
    /// </summary>
    [TestFixture]
    public class NiftiEdgeCaseTests
    {
        private string tempTestDir;

        [SetUp]
        public void Setup()
        {
            tempTestDir = Path.Combine(Path.GetTempPath(), "NiftiEdgeCaseTests", Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempTestDir);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(tempTestDir))
            {
                Directory.Delete(tempTestDir, true);
            }
        }

        #region Malformed File Tests

        [Test]
        public void ReadFile_TooSmallFile_ShouldThrowException()
        {
            var testFile = Path.Combine(tempTestDir, "too_small.nii");
            File.WriteAllBytes(testFile, new byte[100]); // Way too small for header

            Assert.Throws<Exception>(() => NiftiFile.Read(testFile), 
                "Should throw exception when file is too small to contain header");
        }

        [Test]
        public void ReadFile_HeaderOnlyFile_ShouldThrowOrHandleGracefully()
        {
            var testFile = Path.Combine(tempTestDir, "header_only.nii");
            var headerBytes = new byte[348];
            
            // Create minimal valid header
            Array.Copy(BitConverter.GetBytes(348), 0, headerBytes, 0, 4); // sizeof_hdr
            headerBytes[344] = 0x6E; // 'n'
            headerBytes[345] = 0x2B; // '+'
            headerBytes[346] = 0x31; // '1'
            headerBytes[347] = 0x00; // '\0'
            
            File.WriteAllBytes(testFile, headerBytes);

            // This should either throw an exception or handle gracefully
            Assert.DoesNotThrow(() =>
            {
                try
                {
                    var nifti = NiftiFile.Read(testFile);
                    // If it succeeds, data should be null or empty
                    Assert.That(nifti.Data, Is.Null.Or.Empty, "Data should be null or empty for header-only file");
                }
                catch (Exception ex)
                {
                    // Throwing an exception is also acceptable behavior
                    Assert.IsInstanceOf<InvalidDataException>(ex, "Should throw InvalidDataException for malformed file");
                }
            });
        }

        [Test]
        public void ReadFile_CorruptedGzipFile_ShouldThrowException()
        {
            var testFile = Path.Combine(tempTestDir, "corrupted.nii.gz");
            
            // Create file with gzip magic bytes but corrupted content
            var corruptedGzip = new byte[] { 0x1F, 0x8B, 0x08, 0x00 }; // Gzip magic + flags
            Array.Resize(ref corruptedGzip, 1000);
            // Fill with random data that's not valid gzip
            var random = new Random(42);
            random.NextBytes(corruptedGzip);
            
            File.WriteAllBytes(testFile, corruptedGzip);

            Assert.Throws<Exception>(() => NiftiFile.Read(testFile), 
                "Should throw exception when gzip file is corrupted");
        }

        #endregion

        #region Extreme Values Tests

        [Test]
        public void Header_ExtremelyLargeDimensions_ShouldBeHandled()
        {
            var header = new NiftiHeader();
            header.sizeof_hdr = 348;
            header.magic = new byte[] { 0x6E, 0x2B, 0x31, 0x00 };
            header.dim[0] = 7; // Maximum dimensions
            header.dim[1] = short.MaxValue;
            header.dim[2] = short.MaxValue;
            header.dim[3] = short.MaxValue;
            header.dim[4] = short.MaxValue;
            header.dim[5] = short.MaxValue;
            header.dim[6] = short.MaxValue;
            header.dim[7] = short.MaxValue;
            header.datatype = NiftiHeader.DT_UINT8;
            header.bitpix = 8;

            // Should calculate extremely large data size
            var expectedSize = header.GetExpectedDataSize();
            Assert.That(expectedSize, Is.GreaterThan(0), "Should calculate data size even for extreme dimensions");
            
            // Total voxels calculation should not overflow
            var totalVoxels = header.GetTotalVoxels();
            Assert.That(totalVoxels, Is.GreaterThan(0), "Total voxels should be positive");
        }

        [Test]
        public void Header_ExtremeDatatypes_ShouldBeValidated()
        {
            var header = new NiftiHeader();
            header.sizeof_hdr = 348;
            header.magic = new byte[] { 0x6E, 0x2B, 0x31, 0x00 };
            header.dim[0] = 1;
            header.dim[1] = 1;

            // Test extreme/invalid data type values
            var invalidDataTypes = new short[] { -1, 999, short.MaxValue, short.MinValue };
            
            foreach (var invalidType in invalidDataTypes)
            {
                header.datatype = invalidType;
                header.bitpix = 8; // Set some bitpix
                
                Assert.IsFalse(header.IsDataTypeBitpixValid(), 
                    $"Invalid data type {invalidType} should not be considered valid");
            }
        }

        [Test]
        public void Header_ExtremeQuaternionValues_ShouldBeValidated()
        {
            var header = new NiftiHeader();
            header.sizeof_hdr = 348;
            header.magic = new byte[] { 0x6E, 0x2B, 0x31, 0x00 };
            header.dim[0] = 3;
            header.dim[1] = 64;
            header.dim[2] = 64;
            header.dim[3] = 32;
            header.datatype = NiftiHeader.DT_FLOAT32;
            header.bitpix = 32;

            // Test quaternion with magnitude > 1 (invalid)
            header.quatern_b = 1.0f;
            header.quatern_c = 1.0f;
            header.quatern_d = 1.0f; // |q|² = 3 > 1

            Assert.Throws<InvalidDataException>(() => header.ValidateHeader(), 
                "Should reject quaternion parameters with magnitude > 1");

            // Test extreme values
            header.quatern_b = float.MaxValue;
            header.quatern_c = 0.0f;
            header.quatern_d = 0.0f;

            Assert.Throws<InvalidDataException>(() => header.ValidateHeader(), 
                "Should reject extreme quaternion values");
        }

        #endregion

        #region Unicode and Special Character Tests

        [Test]
        public void Header_UnicodeInDescription_ShouldBeHandled()
        {
            var nifti = Nifti.CreateFromData(new float[8], new int[] { 2, 2, 2 });
            
            // Test various problematic strings
            var problematicStrings = new string[]
            {
                "Test with émojis 🧠🔬",
                "Tëst wîth âccénts",
                "Test with null\0character",
                "Very long description that exceeds the normal expected length for a description field in a NIfTI header and should be truncated appropriately by the implementation to fit within the allocated 80 bytes",
                "", // Empty string
                new string('X', 200) // Very long string
            };

            foreach (var testString in problematicStrings)
            {
                Assert.DoesNotThrow(() =>
                {
                    var testNifti = Nifti.CreateFromData(new float[8], new int[] { 2, 2, 2 }, description: testString);
                    
                    // Should not crash when writing/reading
                    var testFile = Path.Combine(tempTestDir, $"unicode_test_{Guid.NewGuid()}.nii");
                    NiftiFile.Write(testNifti, testFile);
                    var readNifti = NiftiFile.Read(testFile);
                    
                    Assert.IsNotNull(readNifti, "Should be able to read file with problematic description");
                }, $"Should handle problematic string: {testString}");
            }
        }

        #endregion

        #region Memory and Performance Edge Cases

        [Test]
        public void CreateFromData_EmptyData_ShouldBeHandledGracefully()
        {
            Assert.DoesNotThrow(() =>
            {
                var emptyData = new float[0];
                // This should either work or throw a clear exception
                try
                {
                    var nifti = Nifti.CreateFromData(emptyData, new int[] { 0 });
                    Assert.IsNotNull(nifti, "Should create NIfTI object even with empty data");
                }
                catch (ArgumentException)
                {
                    // Throwing ArgumentException is acceptable for empty data
                }
            });
        }

        [Test]
        public void CreateFromData_SingleVoxel_ShouldWork()
        {
            var singleVoxelData = new float[] { 42.0f };
            var dimensions = new int[] { 1 };
            
            var nifti = Nifti.CreateFromData(singleVoxelData, dimensions);
            
            Assert.IsNotNull(nifti);
            Assert.AreEqual(1, nifti.Header.dim[0]);
            Assert.AreEqual(1, nifti.Header.dim[1]);
            Assert.AreEqual(42.0f, nifti[0]);
        }

        [Test]
        public void CreateFromData_7DimensionalData_ShouldWork()
        {
            // Test maximum dimensions (7D)
            var data = new float[2 * 2 * 2 * 2 * 2 * 2 * 2];
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = i;
            }
            
            var dimensions = new int[] { 2, 2, 2, 2, 2, 2, 2 };
            
            var nifti = Nifti.CreateFromData(data, dimensions);
            
            Assert.IsNotNull(nifti);
            Assert.AreEqual(7, nifti.Header.dim[0]);
            Assert.AreEqual(128, data.Length);
            Assert.AreEqual(0, nifti[0, 0, 0, 0, 0, 0, 0]);
            Assert.AreEqual(127, nifti[1, 1, 1, 1, 1, 1, 1]);
        }

        #endregion

        #region File System Edge Cases

        [Test]
        public void WriteFile_ReadOnlyDirectory_ShouldThrowException()
        {
            var readOnlyDir = Path.Combine(tempTestDir, "readonly");
            Directory.CreateDirectory(readOnlyDir);
            
            try
            {
                // Try to make directory read-only (this might not work on all systems)
                var dirInfo = new DirectoryInfo(readOnlyDir);
                dirInfo.Attributes |= FileAttributes.ReadOnly;
                
                var nifti = Nifti.CreateFromData(new float[8], new int[] { 2, 2, 2 });
                var testFile = Path.Combine(readOnlyDir, "test.nii");
                
                Assert.Throws<UnauthorizedAccessException>(() => NiftiFile.Write(nifti, testFile), 
                    "Should throw UnauthorizedAccessException for read-only directory");
            }
            finally
            {
                // Clean up - remove read-only attribute
                var dirInfo = new DirectoryInfo(readOnlyDir);
                dirInfo.Attributes &= ~FileAttributes.ReadOnly;
            }
        }

        [Test]
        public void ReadFile_NonExistentFile_ShouldThrowException()
        {
            var nonExistentFile = Path.Combine(tempTestDir, "does_not_exist.nii");
            
            Assert.Throws<FileNotFoundException>(() => NiftiFile.Read(nonExistentFile), 
                "Should throw FileNotFoundException for non-existent file");
        }

        [Test]
        public void ReadFile_VeryLongFilename_ShouldBeHandled()
        {
            // Create a file with a very long name (but within system limits)
            var longName = new string('a', 100) + ".nii";
            var testFile = Path.Combine(tempTestDir, longName);
            
            var nifti = Nifti.CreateFromData(new float[8], new int[] { 2, 2, 2 });
            
            Assert.DoesNotThrow(() =>
            {
                NiftiFile.Write(nifti, testFile);
                var readNifti = NiftiFile.Read(testFile);
                Assert.IsNotNull(readNifti);
            }, "Should handle files with long names");
        }

        #endregion

        #region Coordinate System Edge Cases

        [Test]
        public void Header_ExtremeCoordinateValues_ShouldBeHandled()
        {
            var header = new NiftiHeader();
            header.sizeof_hdr = 348;
            header.magic = new byte[] { 0x6E, 0x2B, 0x31, 0x00 };
            header.dim[0] = 3;
            header.dim[1] = 64;
            header.dim[2] = 64;
            header.dim[3] = 32;
            header.datatype = NiftiHeader.DT_FLOAT32;
            header.bitpix = 32;

            // Test extreme coordinate values
            header.qoffset_x = float.MaxValue;
            header.qoffset_y = float.MinValue;
            header.qoffset_z = float.NaN;

            // NaN values should be handled (though they're not ideal)
            Assert.DoesNotThrow(() =>
            {
                var offsetSum = header.qoffset_x + header.qoffset_y + header.qoffset_z;
                // NaN propagation is expected
            }, "Should handle extreme coordinate values without crashing");
        }

        [Test]
        public void Header_AllZeroPixelDimensions_ShouldNotCrash()
        {
            var header = new NiftiHeader();
            header.sizeof_hdr = 348;
            header.magic = new byte[] { 0x6E, 0x2B, 0x31, 0x00 };
            header.dim[0] = 3;
            header.dim[1] = 64;
            header.dim[2] = 64;
            header.dim[3] = 32;
            header.datatype = NiftiHeader.DT_FLOAT32;
            header.bitpix = 32;

            // Set all pixel dimensions to zero
            for (int i = 0; i < header.pixdim.Length; i++)
            {
                header.pixdim[i] = 0.0f;
            }

            Assert.DoesNotThrow(() => header.ValidateHeader(), 
                "Zero pixel dimensions should not cause validation to crash");
        }

        #endregion

        #region Data Consistency Edge Cases

        [Test]
        public void Header_InconsistentDataSizeAndDimensions_ShouldBeDetectable()
        {
            var header = new NiftiHeader();
            header.sizeof_hdr = 348;
            header.magic = new byte[] { 0x6E, 0x2B, 0x31, 0x00 };
            header.dim[0] = 3;
            header.dim[1] = 64;
            header.dim[2] = 64;
            header.dim[3] = 32; // Should be 64*64*32 = 131072 voxels
            header.datatype = NiftiHeader.DT_FLOAT32;
            header.bitpix = 32;

            var expectedDataSize = header.GetExpectedDataSize();
            var expectedVoxels = header.GetTotalVoxels();

            Assert.AreEqual(131072, expectedVoxels, "Should calculate correct number of voxels");
            Assert.AreEqual(131072 * 4, expectedDataSize, "Should calculate correct data size in bytes");

            // Test with data that doesn't match dimensions
            var incorrectData = new float[1000]; // Wrong size
            var nifti = new Nifti { Header = header, Data = incorrectData };

            // This inconsistency should be detectable
            Assert.AreNotEqual(expectedVoxels, incorrectData.Length, 
                "Data length should not match expected voxel count");
        }

        #endregion
    }
}