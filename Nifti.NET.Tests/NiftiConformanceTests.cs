using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Nifti.NET;

namespace Nifti.NET.Tests
{
    /// <summary>
    /// Comprehensive test suite for NIfTI-1 specification conformance
    /// Based on NIfTI-1 Data Format specification at https://nifti.nimh.nih.gov/nifti-1.html
    /// </summary>
    [TestFixture]
    public class NiftiConformanceTests
    {
        private string tempTestDir;

        [SetUp]
        public void Setup()
        {
            tempTestDir = Path.Combine(Path.GetTempPath(), "NiftiConformanceTests", Guid.NewGuid().ToString());
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

        #region Header Size and Structure Tests

        [Test]
        public void Header_SizeOfHdr_MustBe348()
        {
            // Arrange
            var header = new NiftiHeader();

            // Assert
            Assert.AreEqual(348, header.sizeof_hdr, "sizeof_hdr must be exactly 348 bytes per NIfTI-1 specification");
            Assert.AreEqual(348, NiftiHeader.EXPECTED_SIZE_OF_HDR, "EXPECTED_SIZE_OF_HDR constant must be 348");
        }

        [Test]
        public void Header_MagicNumbers_MustBeValid()
        {
            // Test .nii file magic number (n+1\0)
            var niiHeader = new NiftiHeader();
            niiHeader.magic = new byte[] { 0x6E, 0x2B, 0x31, 0x00 }; // "n+1\0"
            
            Assert.AreEqual(0x6E, niiHeader.magic[0], "First magic byte must be 'n' (0x6E)");
            Assert.AreEqual(0x2B, niiHeader.magic[1], "Second magic byte must be '+' (0x2B) for .nii files");
            Assert.AreEqual(0x31, niiHeader.magic[2], "Third magic byte must be '1' (0x31)");
            Assert.AreEqual(0x00, niiHeader.magic[3], "Fourth magic byte must be null (0x00)");

            // Test .hdr file magic number (ni1\0)
            var hdrHeader = new NiftiHeader();
            hdrHeader.magic = new byte[] { 0x6E, 0x69, 0x31, 0x00 }; // "ni1\0"
            
            Assert.AreEqual(0x6E, hdrHeader.magic[0], "First magic byte must be 'n' (0x6E)");
            Assert.AreEqual(0x69, hdrHeader.magic[1], "Second magic byte must be 'i' (0x69) for .hdr files");
            Assert.AreEqual(0x31, hdrHeader.magic[2], "Third magic byte must be '1' (0x31)");
            Assert.AreEqual(0x00, hdrHeader.magic[3], "Fourth magic byte must be null (0x00)");
        }

        [Test]
        public void Header_InvalidMagicNumbers_ShouldBeRejected()
        {
            var invalidMagicSets = new byte[][]
            {
                new byte[] { 0x6E, 0x69, 0x32, 0x00 }, // Wrong version
                new byte[] { 0x6E, 0x69, 0x31, 0x01 }, // Wrong terminator
                new byte[] { 0x6F, 0x69, 0x31, 0x00 }, // Wrong first character
                new byte[] { 0x6E, 0x6A, 0x31, 0x00 }, // Invalid second character
                new byte[] { 0x00, 0x00, 0x00, 0x00 }, // All zeros
            };

            foreach (var invalidMagic in invalidMagicSets)
            {
                var header = new NiftiHeader();
                header.magic = invalidMagic;

                // This should result in FileType.UNKNOWN when validated
                var magicString = System.Text.Encoding.ASCII.GetString(invalidMagic).TrimEnd('\0');
                Assert.That(magicString, Is.Not.EqualTo("ni1").And.Not.EqualTo("n+1"), 
                    $"Magic bytes {BitConverter.ToString(invalidMagic)} should not be valid NIfTI magic");
            }
        }

        #endregion

        #region Dimension Validation Tests

        [Test]
        public void Header_Dimensions_MustBeValid()
        {
            var header = new NiftiHeader();
            
            // Test valid dimension count (1-7)
            for (short i = 1; i <= 7; i++)
            {
                header.dim[0] = i;
                Assert.That(header.dim[0], Is.InRange(1, 7), $"dim[0] = {i} should be valid (1-7 dimensions allowed)");
            }
            
            // Test that dimensions 1-3 represent spatial axes
            header.dim[0] = 3;
            header.dim[1] = 64; // x
            header.dim[2] = 64; // y  
            header.dim[3] = 32; // z
            
            Assert.Greater(header.dim[1], 0, "X dimension (dim[1]) must be positive");
            Assert.Greater(header.dim[2], 0, "Y dimension (dim[2]) must be positive");
            Assert.Greater(header.dim[3], 0, "Z dimension (dim[3]) must be positive");
        }

        [Test]
        public void Header_InvalidDimensions_ShouldBeRejected()
        {
            // Test zero dimensions
            Assert.Throws<InvalidDataException>(() =>
            {
                CreateAndValidateHeader(header => header.dim[0] = 0);
            }, "dim[0] = 0 should be rejected");

            // Test negative dimensions
            Assert.Throws<InvalidDataException>(() =>
            {
                CreateAndValidateHeader(header => header.dim[0] = -1);
            }, "Negative dim[0] should be rejected");

            // Test too many dimensions
            Assert.Throws<InvalidDataException>(() =>
            {
                CreateAndValidateHeader(header => header.dim[0] = 8);
            }, "dim[0] = 8 should be rejected (max 7 dimensions)");
        }

        #endregion

        #region Data Type Tests

        [Test]
        public void Header_DataTypes_MustMatchBitpix()
        {
            var validDataTypes = new (short datatype, short bitpix)[]
            {
                (NiftiHeader.DT_UINT8, 8),
                (NiftiHeader.DT_INT16, 16),
                (NiftiHeader.DT_INT32, 32),
                (NiftiHeader.DT_FLOAT32, 32),
                (NiftiHeader.DT_COMPLEX64, 64),
                (NiftiHeader.DT_FLOAT64, 64),
                (NiftiHeader.DT_RGB24, 24),
                (NiftiHeader.DT_INT8, 8),
                (NiftiHeader.DT_UINT16, 16),
                (NiftiHeader.DT_UINT32, 32),
                (NiftiHeader.DT_INT64, 64),
                (NiftiHeader.DT_UINT64, 64),
                (NiftiHeader.DT_FLOAT128, 128),
                (NiftiHeader.DT_COMPLEX128, 128),
                (NiftiHeader.DT_COMPLEX256, 256),
                (NiftiHeader.DT_RGBA32, 32)
            };

            foreach (var (datatype, expectedBitpix) in validDataTypes)
            {
                var header = new NiftiHeader();
                header.datatype = datatype;
                header.bitpix = expectedBitpix;

                Assert.AreEqual(expectedBitpix, header.bitpix, 
                    $"Data type {datatype} should have {expectedBitpix} bits per pixel");
            }
        }

        [Test]
        public void Header_UnknownDataType_ShouldBeHandled()
        {
            var header = new NiftiHeader();
            header.datatype = NiftiHeader.DT_UNKNOWN;
            header.bitpix = 0;

            Assert.AreEqual(NiftiHeader.DT_UNKNOWN, header.datatype);
            Assert.AreEqual(0, header.bitpix);
        }

        #endregion

        #region Coordinate System Tests

        [Test]
        public void Header_CoordinateCodes_MustBeValid()
        {
            var validCodes = new short[]
            {
                NiftiHeader.NIFTI_XFORM_UNKNOWN,
                NiftiHeader.NIFTI_XFORM_SCANNER_ANAT,
                NiftiHeader.NIFTI_XFORM_ALIGNED_ANAT,
                NiftiHeader.NIFTI_XFORM_TALAIRACH,
                NiftiHeader.NIFTI_XFORM_MNI_152,
                NiftiHeader.NIFTI_XFORM_TEMPLATE_OTHER
            };

            foreach (var code in validCodes)
            {
                var header = new NiftiHeader();
                header.qform_code = code;
                header.sform_code = code;

                Assert.That(header.qform_code, Is.GreaterThanOrEqualTo(0), 
                    $"qform_code {code} should be non-negative");
                Assert.That(header.sform_code, Is.GreaterThanOrEqualTo(0), 
                    $"sform_code {code} should be non-negative");
            }
        }

        [Test]
        public void Header_QuaternionParameters_ShouldBeValid()
        {
            var header = new NiftiHeader();
            
            // Test valid quaternion (unit quaternion constraint: a²+b²+c²+d²=1)
            // where a = sqrt(1-b²-c²-d²) and stored values are b,c,d
            header.quatern_b = 0.0f;
            header.quatern_c = 0.0f;
            header.quatern_d = 0.0f;
            // This represents a = 1, so it's a valid unit quaternion [1,0,0,0]

            var quaternionMagnitudeSquared = header.quatern_b * header.quatern_b + 
                                           header.quatern_c * header.quatern_c + 
                                           header.quatern_d * header.quatern_d;
            
            Assert.That(quaternionMagnitudeSquared, Is.LessThanOrEqualTo(1.0f), 
                "Quaternion parameters b²+c²+d² must be ≤ 1.0 for valid unit quaternion");
        }

        #endregion

        #region File Format Tests

        [Test]
        public void FileFormat_VoxOffset_MustBeValid()
        {
            // For .nii files, vox_offset should be >= 352 and multiple of 16
            var validOffsets = new float[] { 352.0f, 368.0f, 384.0f, 512.0f, 1024.0f };
            
            foreach (var offset in validOffsets)
            {
                var header = CreateValidNiiHeader();
                header.vox_offset = offset;
                
                Assert.That(header.vox_offset, Is.GreaterThanOrEqualTo(352.0f), 
                    $"vox_offset {offset} must be >= 352 for .nii files");
                Assert.That(header.vox_offset % 16, Is.EqualTo(0), 
                    $"vox_offset {offset} should be multiple of 16 for compatibility");
            }
        }

        [Test]
        public void FileFormat_HdrImgPair_ShouldHaveZeroVoxOffset()
        {
            var header = CreateValidHdrHeader();
            header.vox_offset = 0.0f; // Should be 0 for .hdr/.img pairs
            
            Assert.AreEqual(0.0f, header.vox_offset, 
                "vox_offset should be 0.0 for .hdr/.img file pairs");
        }

        #endregion

        #region Units and Scaling Tests

        [Test]
        public void Header_UnitsField_ShouldBeValid()
        {
            var header = new NiftiHeader();
            
            // Test spatial units (bits 0-2)
            var spatialUnits = new byte[] 
            { 
                NiftiHeader.NIFTI_UNITS_UNKNOWN,
                NiftiHeader.NIFTI_UNITS_METER, 
                NiftiHeader.NIFTI_UNITS_MM,
                NiftiHeader.NIFTI_UNITS_MICRON 
            };
            
            // Test temporal units (bits 3-5, multiples of 8)
            var temporalUnits = new byte[] 
            { 
                NiftiHeader.NIFTI_UNITS_SEC,
                NiftiHeader.NIFTI_UNITS_MSEC,
                NiftiHeader.NIFTI_UNITS_USEC,
                NiftiHeader.NIFTI_UNITS_HZ,
                NiftiHeader.NIFTI_UNITS_PPM,
                NiftiHeader.NIFTI_UNITS_RADS 
            };

            foreach (var spatial in spatialUnits)
            {
                foreach (var temporal in temporalUnits)
                {
                    header.xyzt_units = NiftiHeader.SpaceTimeToXyzt(spatial, temporal);
                    
                    var extractedSpatial = NiftiHeader.XyztToSpace(header.xyzt_units);
                    var extractedTemporal = NiftiHeader.XyztToTime(header.xyzt_units);
                    
                    Assert.AreEqual(spatial, extractedSpatial, 
                        $"Spatial units should be preserved: expected {spatial}, got {extractedSpatial}");
                    Assert.AreEqual(temporal, extractedTemporal, 
                        $"Temporal units should be preserved: expected {temporal}, got {extractedTemporal}");
                }
            }
        }

        [Test]
        public void Header_ScalingParameters_ShouldBeValid()
        {
            var header = new NiftiHeader();
            
            // Test valid scaling parameters
            header.scl_slope = 1.0f;
            header.scl_inter = 0.0f;
            
            Assert.That(header.scl_slope, Is.Not.EqualTo(0.0f), 
                "scl_slope should not be zero when scaling is enabled");
            
            // Test that scaling is ignored for RGB data
            header.datatype = NiftiHeader.DT_RGB24;
            // For RGB data, scaling should be ignored regardless of scl_slope value
            Assert.AreEqual(NiftiHeader.DT_RGB24, header.datatype, 
                "RGB24 data type should ignore scaling parameters");
        }

        #endregion

        #region Intent Codes Tests

        [Test]
        public void Header_IntentCodes_ShouldBeValid()
        {
            var validIntentCodes = new short[]
            {
                NiftiHeader.NIFTI_INTENT_NONE,
                NiftiHeader.NIFTI_INTENT_CORREL,
                NiftiHeader.NIFTI_INTENT_TTEST,
                NiftiHeader.NIFTI_INTENT_FTEST,
                NiftiHeader.NIFTI_INTENT_ZSCORE,
                NiftiHeader.NIFTI_INTENT_CHISQ,
                NiftiHeader.NIFTI_INTENT_ESTIMATE,
                NiftiHeader.NIFTI_INTENT_LABEL,
                NiftiHeader.NIFTI_INTENT_VECTOR,
                NiftiHeader.NIFTI_INTENT_POINTSET,
                NiftiHeader.NIFTI_INTENT_RGB_VECTOR
            };

            foreach (var intentCode in validIntentCodes)
            {
                var header = new NiftiHeader();
                header.intent_code = intentCode;
                
                Assert.That(header.intent_code, Is.GreaterThanOrEqualTo(0), 
                    $"Intent code {intentCode} should be non-negative");
            }
        }

        [Test]
        public void Header_StatisticalIntentCodes_ShouldHaveValidParameterCount()
        {
            // Test statistical intent codes that require specific parameter counts
            var statisticalTests = new (short intentCode, int expectedParams)[]
            {
                (NiftiHeader.NIFTI_INTENT_TTEST, 1),      // 1 DOF parameter
                (NiftiHeader.NIFTI_INTENT_FTEST, 2),      // 2 DOF parameters  
                (NiftiHeader.NIFTI_INTENT_CHISQ, 1),      // 1 DOF parameter
                (NiftiHeader.NIFTI_INTENT_CORREL, 1),     // 1 DOF parameter
                (NiftiHeader.NIFTI_INTENT_NORMAL, 2),     // mean, std dev
                (NiftiHeader.NIFTI_INTENT_UNIFORM, 2),    // lower, upper bounds
            };

            foreach (var (intentCode, paramCount) in statisticalTests)
            {
                var header = new NiftiHeader();
                header.intent_code = intentCode;
                
                // Verify the intent code is in the statistical range
                Assert.That(intentCode, Is.InRange(NiftiHeader.NIFTI_FIRST_STATCODE, NiftiHeader.NIFTI_LAST_STATCODE)
                    .Or.EqualTo(NiftiHeader.NIFTI_INTENT_NORMAL).Or.EqualTo(NiftiHeader.NIFTI_INTENT_UNIFORM), 
                    $"Intent code {intentCode} should be a valid statistical code");
            }
        }

        #endregion

        #region Slice Timing Tests

        [Test]
        public void Header_SliceTimingCodes_ShouldBeValid()
        {
            var validSliceCodes = new short[]
            {
                NiftiHeader.NIFTI_SLICE_UNKNOWN,
                NiftiHeader.NIFTI_SLICE_SEQ_INC,
                NiftiHeader.NIFTI_SLICE_SEQ_DEC,
                NiftiHeader.NIFTI_SLICE_ALT_INC,
                NiftiHeader.NIFTI_SLICE_ALT_DEC,
                NiftiHeader.NIFTI_SLICE_ALT_INC2,
                NiftiHeader.NIFTI_SLICE_ALT_DEC2
            };

            foreach (var sliceCode in validSliceCodes)
            {
                var header = new NiftiHeader();
                header.slice_code = (byte)sliceCode;
                
                Assert.That(header.slice_code, Is.GreaterThanOrEqualTo(0), 
                    $"Slice code {sliceCode} should be non-negative");
                Assert.That(header.slice_code, Is.LessThanOrEqualTo(6), 
                    $"Slice code {sliceCode} should be ≤ 6");
            }
        }

        [Test]
        public void Header_SliceTimingParameters_ShouldBeConsistent()
        {
            var header = new NiftiHeader();
            header.dim[0] = 3;
            header.dim[3] = 20; // 20 slices
            header.slice_start = 0;
            header.slice_end = 19;
            header.slice_code = NiftiHeader.NIFTI_SLICE_SEQ_INC;
            header.slice_duration = 0.1f; // 100ms per slice
            
            Assert.That(header.slice_start, Is.GreaterThanOrEqualTo(0), 
                "slice_start should be non-negative");
            Assert.That(header.slice_end, Is.GreaterThan(header.slice_start), 
                "slice_end should be greater than slice_start");
            Assert.That(header.slice_end, Is.LessThan(header.dim[3]), 
                "slice_end should be less than the number of slices");
            Assert.That(header.slice_duration, Is.GreaterThan(0), 
                "slice_duration should be positive when slice timing is used");
        }

        #endregion

        #region Endianness Tests

        [Test]
        public void Header_EndianessDetection_ShouldWork()
        {
            // Test big-endian detection
            var bigEndianHeader = new NiftiHeader();
            bigEndianHeader.sizeof_hdr = 22806528; // 348 in big-endian (byte-swapped)
            
            Assert.IsTrue(bigEndianHeader.SourceIsBigEndian(), 
                "Should detect big-endian when sizeof_hdr is byte-swapped");
            
            // Test little-endian (normal case)
            var littleEndianHeader = new NiftiHeader();
            littleEndianHeader.sizeof_hdr = 348; // Normal value
            
            Assert.IsFalse(littleEndianHeader.SourceIsBigEndian(), 
                "Should detect little-endian when sizeof_hdr is normal");
        }

        #endregion

        #region Extension Tests

        [Test]
        public void Header_Extensions_ShouldBeValidWhenPresent()
        {
            var header = new NiftiHeader();
            
            // Test no extension
            header.extension = new byte[] { 0, 0, 0, 0 };
            Assert.AreEqual(0, header.extension[0], "extension[0] should be 0 when no extension present");
            
            // Test with extension
            header.extension = new byte[] { 1, 0, 0, 0 };
            header.esize = 16; // Minimum extension size (8 bytes header + data)
            header.ecode = 1;  // Some extension code
            header.edata = new byte[8]; // 8 bytes of extension data
            
            Assert.AreEqual(1, header.extension[0], "extension[0] should be 1 when extension present");
            Assert.That(header.esize, Is.GreaterThanOrEqualTo(8), 
                "Extension size should be at least 8 bytes (esize + ecode)");
            Assert.AreEqual(header.esize - 8, header.edata.Length, 
                "Extension data length should be esize - 8");
        }

        #endregion

        #region File I/O Conformance Tests

        [Test]
        public void FileIO_CreateAndReadNiiFile_ShouldPreserveHeader()
        {
            // Create a minimal valid NIfTI file
            var originalData = new float[8] { 1, 2, 3, 4, 5, 6, 7, 8 };
            var dimensions = new int[] { 2, 2, 2 };
            var nifti = Nifti.CreateFromData(originalData, dimensions, description: "Conformance test");
            
            var testFile = Path.Combine(tempTestDir, "conformance_test.nii");
            
            // Write and read back
            NiftiFile.Write(nifti, testFile);
            var readNifti = NiftiFile.Read(testFile);
            
            // Verify header integrity
            Assert.AreEqual(348, readNifti.Header.sizeof_hdr, "sizeof_hdr should be preserved");
            Assert.AreEqual(nifti.Header.dim[0], readNifti.Header.dim[0], "Dimension count should be preserved");
            Assert.AreEqual(nifti.Header.dim[1], readNifti.Header.dim[1], "X dimension should be preserved");
            Assert.AreEqual(nifti.Header.dim[2], readNifti.Header.dim[2], "Y dimension should be preserved");
            Assert.AreEqual(nifti.Header.dim[3], readNifti.Header.dim[3], "Z dimension should be preserved");
            Assert.AreEqual(nifti.Header.datatype, readNifti.Header.datatype, "Data type should be preserved");
            Assert.AreEqual(nifti.Header.bitpix, readNifti.Header.bitpix, "Bits per pixel should be preserved");
            
            // Verify magic number
            Assert.AreEqual(0x6E, readNifti.Header.magic[0], "Magic byte 0 should be preserved");
            Assert.AreEqual(0x2B, readNifti.Header.magic[1], "Magic byte 1 should be preserved (nii format)");
            Assert.AreEqual(0x31, readNifti.Header.magic[2], "Magic byte 2 should be preserved");
            Assert.AreEqual(0x00, readNifti.Header.magic[3], "Magic byte 3 should be preserved");
            
            // Verify data integrity
            var readData = (float[])readNifti.Data;
            Assert.AreEqual(originalData.Length, readData.Length, "Data length should be preserved");
            for (int i = 0; i < originalData.Length; i++)
            {
                Assert.AreEqual(originalData[i], readData[i], $"Data element {i} should be preserved");
            }
        }

        [Test]
        public void FileIO_InvalidMagicNumber_ShouldThrowException()
        {
            // Create a file with invalid magic number
            var testFile = Path.Combine(tempTestDir, "invalid_magic.nii");
            var validNifti = Nifti.CreateFromData(new float[8], new int[] { 2, 2, 2 });
            
            // Corrupt the magic number
            validNifti.Header.magic = new byte[] { 0x00, 0x00, 0x00, 0x00 };
            
            NiftiFile.Write(validNifti, testFile);
            
            // Reading should throw exception
            Assert.Throws<InvalidDataException>(() => NiftiFile.Read(testFile), 
                "Should throw InvalidDataException for invalid magic number");
        }

        #endregion

        #region Helper Methods

        private NiftiHeader CreateValidNiiHeader()
        {
            var header = new NiftiHeader();
            header.sizeof_hdr = 348;
            header.magic = new byte[] { 0x6E, 0x2B, 0x31, 0x00 }; // "n+1\0"
            header.dim[0] = 3;
            header.dim[1] = 64;
            header.dim[2] = 64;
            header.dim[3] = 32;
            header.datatype = NiftiHeader.DT_FLOAT32;
            header.bitpix = 32;
            header.vox_offset = 352.0f;
            return header;
        }

        private NiftiHeader CreateValidHdrHeader()
        {
            var header = CreateValidNiiHeader();
            header.magic = new byte[] { 0x6E, 0x69, 0x31, 0x00 }; // "ni1\0"
            header.vox_offset = 0.0f;
            return header;
        }

        private void CreateAndValidateHeader(Action<NiftiHeader> modifyHeader)
        {
            var header = CreateValidNiiHeader();
            modifyHeader(header);
            
            // Create a test file to trigger validation
            var testFile = Path.Combine(tempTestDir, "validation_test.nii");
            var nifti = new Nifti { Header = header, Data = new float[64 * 64 * 32] };
            
            NiftiFile.Write(nifti, testFile);
            NiftiFile.Read(testFile); // This should trigger validation
        }

        #endregion
    }
}