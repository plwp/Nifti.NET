using System;
using System.IO;
using NUnit.Framework;
using Nifti.NET;

namespace Nifti.NET.Tests
{
    /// <summary>
    /// Tests to verify complete NIfTI-1 specification compliance
    /// These tests validate against the official NIfTI-1 specification requirements
    /// </summary>
    [TestFixture]
    public class NiftiSpecificationComplianceTests
    {
        private string tempTestDir;

        [SetUp]
        public void Setup()
        {
            tempTestDir = Path.Combine(Path.GetTempPath(), "NiftiSpecComplianceTests", Guid.NewGuid().ToString());
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

        #region Specification Section 1: Header Structure Tests

        [Test]
        public void Spec_HeaderSize_MustBe348Bytes()
        {
            // NIfTI-1 spec: "The header is exactly 348 bytes long"
            var header = new NiftiHeader();
            Assert.AreEqual(348, header.sizeof_hdr, "Header size must be exactly 348 bytes per NIfTI-1 spec");
            Assert.AreEqual(348, NiftiHeader.EXPECTED_SIZE_OF_HDR, "Expected header size constant must be 348");
        }

        [Test]
        public void Spec_MagicNumbers_MustConformToSpecification()
        {
            // NIfTI-1 spec: Magic numbers must be "ni1\0" or "n+1\0"
            
            // Test .nii format (single file)
            var niiHeader = new NiftiHeader();
            niiHeader.magic = new byte[] { 0x6E, 0x2B, 0x31, 0x00 }; // "n+1\0"
            Assert.IsTrue(niiHeader.HasValidMagicNumber(), "n+1\\0 should be valid magic for .nii files");
            Assert.IsTrue(niiHeader.IsNiiFile(), "Should be identified as .nii file");
            Assert.IsFalse(niiHeader.IsHdrFile(), "Should not be identified as .hdr file");

            // Test .hdr format (dual file)
            var hdrHeader = new NiftiHeader();
            hdrHeader.magic = new byte[] { 0x6E, 0x69, 0x31, 0x00 }; // "ni1\0"
            Assert.IsTrue(hdrHeader.HasValidMagicNumber(), "ni1\\0 should be valid magic for .hdr files");
            Assert.IsTrue(hdrHeader.IsHdrFile(), "Should be identified as .hdr file");
            Assert.IsFalse(hdrHeader.IsNiiFile(), "Should not be identified as .nii file");
        }

        #endregion

        #region Specification Section 2: Data Types Tests

        [Test]
        public void Spec_DataTypes_MustHaveCorrectBitpixValues()
        {
            // NIfTI-1 spec: Table of data types and their bit counts
            var specDataTypes = new (short datatype, short expectedBitpix)[]
            {
                // Original ANALYZE 7.5 types
                (NiftiHeader.DT_UINT8, 8),
                (NiftiHeader.DT_INT16, 16),
                (NiftiHeader.DT_INT32, 32),
                (NiftiHeader.DT_FLOAT32, 32),
                (NiftiHeader.DT_COMPLEX64, 64),
                (NiftiHeader.DT_FLOAT64, 64),
                (NiftiHeader.DT_RGB24, 24),
                
                // NIfTI-1 extensions
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

            foreach (var (datatype, expectedBitpix) in specDataTypes)
            {
                var header = new NiftiHeader();
                header.datatype = datatype;
                header.bitpix = expectedBitpix;
                
                Assert.IsTrue(header.IsDataTypeBitpixValid(), 
                    $"Data type {datatype} with bitpix {expectedBitpix} should be valid per NIfTI-1 spec");
                
                Assert.AreEqual(expectedBitpix / 8, header.GetBytesPerVoxel(),
                    $"Bytes per voxel calculation should be correct for data type {datatype}");
            }
        }

        [Test]
        public void Spec_DataTypeConstants_MustMatchSpecification()
        {
            // Verify that our constants match the NIfTI-1 specification values
            Assert.AreEqual(0, NiftiHeader.DT_UNKNOWN, "DT_UNKNOWN must be 0");
            Assert.AreEqual(1, NiftiHeader.DT_BINARY, "DT_BINARY must be 1");
            Assert.AreEqual(2, NiftiHeader.DT_UINT8, "DT_UINT8 must be 2");
            Assert.AreEqual(4, NiftiHeader.DT_INT16, "DT_INT16 must be 4");
            Assert.AreEqual(8, NiftiHeader.DT_INT32, "DT_INT32 must be 8");
            Assert.AreEqual(16, NiftiHeader.DT_FLOAT32, "DT_FLOAT32 must be 16");
            Assert.AreEqual(32, NiftiHeader.DT_COMPLEX64, "DT_COMPLEX64 must be 32");
            Assert.AreEqual(64, NiftiHeader.DT_FLOAT64, "DT_FLOAT64 must be 64");
            Assert.AreEqual(128, NiftiHeader.DT_RGB24, "DT_RGB24 must be 128");
            Assert.AreEqual(256, NiftiHeader.DT_INT8, "DT_INT8 must be 256");
            Assert.AreEqual(512, NiftiHeader.DT_UINT16, "DT_UINT16 must be 512");
            Assert.AreEqual(768, NiftiHeader.DT_UINT32, "DT_UINT32 must be 768");
            Assert.AreEqual(1024, NiftiHeader.DT_INT64, "DT_INT64 must be 1024");
            Assert.AreEqual(1280, NiftiHeader.DT_UINT64, "DT_UINT64 must be 1280");
            Assert.AreEqual(1536, NiftiHeader.DT_FLOAT128, "DT_FLOAT128 must be 1536");
            Assert.AreEqual(1792, NiftiHeader.DT_COMPLEX128, "DT_COMPLEX128 must be 1792");
            Assert.AreEqual(2048, NiftiHeader.DT_COMPLEX256, "DT_COMPLEX256 must be 2048");
            Assert.AreEqual(2304, NiftiHeader.DT_RGBA32, "DT_RGBA32 must be 2304");
        }

        #endregion

        #region Specification Section 3: Dimensions Tests

        [Test]
        public void Spec_Dimensions_MustConformToSpecification()
        {
            // NIfTI-1 spec: dim[0] = number of dimensions (1-7)
            // dim[1] = X, dim[2] = Y, dim[3] = Z, dim[4] = time, dim[5-7] = additional
            
            for (short numDims = 1; numDims <= 7; numDims++)
            {
                var header = CreateValidHeader();
                header.dim[0] = numDims;
                
                // Set appropriate dimension values
                for (int i = 1; i <= numDims; i++)
                {
                    header.dim[i] = (short)(10 + i); // Arbitrary positive values
                }
                
                Assert.DoesNotThrow(() => header.ValidateHeader(), 
                    $"Header with {numDims} dimensions should be valid");
                
                Assert.AreEqual(numDims, header.dim[0], 
                    $"dim[0] should equal number of dimensions ({numDims})");
            }
        }

        [Test]
        public void Spec_SpatialDimensions_MustBePositive()
        {
            // NIfTI-1 spec: Spatial dimensions (X, Y, Z) must be positive
            var header = CreateValidHeader();
            header.dim[0] = 3;
            
            var invalidDimensionValues = new short[] { 0, -1, -100 };
            
            foreach (var invalidValue in invalidDimensionValues)
            {
                // Test each spatial dimension
                for (int spatialDim = 1; spatialDim <= 3; spatialDim++)
                {
                    var testHeader = CreateValidHeader();
                    testHeader.dim[0] = 3;
                    testHeader.dim[1] = 64;
                    testHeader.dim[2] = 64;
                    testHeader.dim[3] = 32;
                    testHeader.dim[spatialDim] = invalidValue;
                    
                    Assert.Throws<InvalidDataException>(() => testHeader.ValidateHeader(),
                        $"Spatial dimension {spatialDim} with value {invalidValue} should be rejected");
                }
            }
        }

        #endregion

        #region Specification Section 4: Coordinate Systems Tests

        [Test]
        public void Spec_CoordinateCodes_MustMatchSpecification()
        {
            // Verify coordinate system code constants match NIfTI-1 spec
            Assert.AreEqual(0, NiftiHeader.NIFTI_XFORM_UNKNOWN, "NIFTI_XFORM_UNKNOWN must be 0");
            Assert.AreEqual(1, NiftiHeader.NIFTI_XFORM_SCANNER_ANAT, "NIFTI_XFORM_SCANNER_ANAT must be 1");
            Assert.AreEqual(2, NiftiHeader.NIFTI_XFORM_ALIGNED_ANAT, "NIFTI_XFORM_ALIGNED_ANAT must be 2");
            Assert.AreEqual(3, NiftiHeader.NIFTI_XFORM_TALAIRACH, "NIFTI_XFORM_TALAIRACH must be 3");
            Assert.AreEqual(4, NiftiHeader.NIFTI_XFORM_MNI_152, "NIFTI_XFORM_MNI_152 must be 4");
        }

        [Test]
        public void Spec_QuaternionParameters_MustFormUnitQuaternion()
        {
            // NIfTI-1 spec: Quaternion parameters (b,c,d) must satisfy b²+c²+d² ≤ 1
            // where a = sqrt(1-b²-c²-d²) ≥ 0
            
            var validQuaternions = new (float b, float c, float d)[]
            {
                (0.0f, 0.0f, 0.0f),      // Identity quaternion
                (1.0f, 0.0f, 0.0f),      // 180° rotation about X
                (0.0f, 1.0f, 0.0f),      // 180° rotation about Y
                (0.0f, 0.0f, 1.0f),      // 180° rotation about Z
                (0.5f, 0.5f, 0.5f),      // Valid unit quaternion
                (0.707f, 0.707f, 0.0f),  // 90° rotation
            };

            foreach (var (b, c, d) in validQuaternions)
            {
                var header = CreateValidHeader();
                header.quatern_b = b;
                header.quatern_c = c;
                header.quatern_d = d;
                
                var magnitudeSquared = b * b + c * c + d * d;
                Assert.That(magnitudeSquared, Is.LessThanOrEqualTo(1.0f).Within(0.001f),
                    $"Quaternion ({b},{c},{d}) should have magnitude ≤ 1");
                
                Assert.DoesNotThrow(() => header.ValidateHeader(),
                    $"Valid quaternion ({b},{c},{d}) should pass validation");
            }
        }

        #endregion

        #region Specification Section 5: File Format Tests

        [Test]
        public void Spec_VoxOffset_MustConformToSpecification()
        {
            // NIfTI-1 spec: For .nii files, vox_offset ≥ 352 and should be multiple of 16
            // For .hdr files, vox_offset should be 0
            
            // Test .nii file requirements
            var niiHeader = CreateValidHeader();
            niiHeader.magic = new byte[] { 0x6E, 0x2B, 0x31, 0x00 }; // "n+1\0"
            
            var validNiiOffsets = new float[] { 352.0f, 368.0f, 384.0f, 512.0f, 1024.0f };
            foreach (var offset in validNiiOffsets)
            {
                niiHeader.vox_offset = offset;
                Assert.DoesNotThrow(() => niiHeader.ValidateHeader(),
                    $"vox_offset {offset} should be valid for .nii files");
            }
            
            // Test invalid offsets for .nii files
            var invalidNiiOffsets = new float[] { 0.0f, 100.0f, 351.0f };
            foreach (var offset in invalidNiiOffsets)
            {
                niiHeader.vox_offset = offset;
                Assert.Throws<InvalidDataException>(() => niiHeader.ValidateHeader(),
                    $"vox_offset {offset} should be invalid for .nii files (must be ≥ 352)");
            }
            
            // Test .hdr file requirements
            var hdrHeader = CreateValidHeader();
            hdrHeader.magic = new byte[] { 0x6E, 0x69, 0x31, 0x00 }; // "ni1\0"
            hdrHeader.vox_offset = 0.0f;
            
            Assert.DoesNotThrow(() => hdrHeader.ValidateHeader(),
                "vox_offset 0.0 should be valid for .hdr files");
        }

        [Test]
        public void Spec_EndianDetection_MustWork()
        {
            // NIfTI-1 spec: Endianness detection via sizeof_hdr field
            
            // Little-endian (normal case)
            var littleEndianHeader = new NiftiHeader();
            littleEndianHeader.sizeof_hdr = 348;
            Assert.IsFalse(littleEndianHeader.SourceIsBigEndian(),
                "Normal sizeof_hdr (348) should indicate little-endian");
            
            // Big-endian (byte-swapped)
            var bigEndianHeader = new NiftiHeader();
            bigEndianHeader.sizeof_hdr = 22806528; // 348 byte-swapped
            Assert.IsTrue(bigEndianHeader.SourceIsBigEndian(),
                "Byte-swapped sizeof_hdr should indicate big-endian");
        }

        #endregion

        #region Specification Section 6: Units Tests

        [Test]
        public void Spec_UnitsEncoding_MustMatchSpecification()
        {
            // NIfTI-1 spec: xyzt_units encodes spatial (bits 0-2) and temporal (bits 3-5) units
            
            var spatialUnits = new byte[]
            {
                NiftiHeader.NIFTI_UNITS_UNKNOWN, // 0
                NiftiHeader.NIFTI_UNITS_METER,   // 1
                NiftiHeader.NIFTI_UNITS_MM,      // 2
                NiftiHeader.NIFTI_UNITS_MICRON   // 3
            };
            
            var temporalUnits = new byte[]
            {
                NiftiHeader.NIFTI_UNITS_SEC,  // 8
                NiftiHeader.NIFTI_UNITS_MSEC, // 16
                NiftiHeader.NIFTI_UNITS_USEC, // 24
                NiftiHeader.NIFTI_UNITS_HZ,   // 32
                NiftiHeader.NIFTI_UNITS_PPM,  // 40
                NiftiHeader.NIFTI_UNITS_RADS  // 48
            };
            
            foreach (var spatial in spatialUnits)
            {
                foreach (var temporal in temporalUnits)
                {
                    var combined = NiftiHeader.SpaceTimeToXyzt(spatial, temporal);
                    var extractedSpatial = NiftiHeader.XyztToSpace(combined);
                    var extractedTemporal = NiftiHeader.XyztToTime(combined);
                    
                    Assert.AreEqual(spatial, extractedSpatial,
                        $"Spatial units should round-trip correctly: {spatial} -> {combined} -> {extractedSpatial}");
                    Assert.AreEqual(temporal, extractedTemporal,
                        $"Temporal units should round-trip correctly: {temporal} -> {combined} -> {extractedTemporal}");
                }
            }
        }

        #endregion

        #region Specification Section 7: Intent Codes Tests

        [Test]
        public void Spec_IntentCodes_MustMatchSpecification()
        {
            // Verify statistical intent code ranges
            Assert.AreEqual(2, NiftiHeader.NIFTI_FIRST_STATCODE, "First statistical code must be 2");
            Assert.AreEqual(24, NiftiHeader.NIFTI_LAST_STATCODE, "Last statistical code must be 24");
            
            // Verify specific statistical codes match spec
            Assert.AreEqual(2, NiftiHeader.NIFTI_INTENT_CORREL, "NIFTI_INTENT_CORREL must be 2");
            Assert.AreEqual(3, NiftiHeader.NIFTI_INTENT_TTEST, "NIFTI_INTENT_TTEST must be 3");
            Assert.AreEqual(4, NiftiHeader.NIFTI_INTENT_FTEST, "NIFTI_INTENT_FTEST must be 4");
            Assert.AreEqual(5, NiftiHeader.NIFTI_INTENT_ZSCORE, "NIFTI_INTENT_ZSCORE must be 5");
            
            // Verify non-statistical codes
            Assert.AreEqual(1001, NiftiHeader.NIFTI_INTENT_ESTIMATE, "NIFTI_INTENT_ESTIMATE must be 1001");
            Assert.AreEqual(1002, NiftiHeader.NIFTI_INTENT_LABEL, "NIFTI_INTENT_LABEL must be 1002");
            Assert.AreEqual(1007, NiftiHeader.NIFTI_INTENT_VECTOR, "NIFTI_INTENT_VECTOR must be 1007");
        }

        #endregion

        #region Specification Section 8: Complete Round-Trip Tests

        [Test]
        public void Spec_CompleteRoundTrip_ShouldPreserveAllFields()
        {
            // Create a comprehensive header with all fields set
            var originalHeader = CreateComprehensiveHeader();
            var originalData = CreateTestData();
            var originalNifti = new Nifti { Header = originalHeader, Data = originalData };
            
            // Write to file and read back
            var testFile = Path.Combine(tempTestDir, "complete_roundtrip.nii");
            NiftiFile.Write(originalNifti, testFile);
            var readNifti = NiftiFile.Read(testFile);
            
            // Verify all critical header fields are preserved
            VerifyHeaderFieldsPreserved(originalHeader, readNifti.Header);
            
            // Verify data integrity
            var originalFloatData = (float[])originalData;
            var readFloatData = (float[])readNifti.Data;
            
            Assert.AreEqual(originalFloatData.Length, readFloatData.Length, "Data length should be preserved");
            for (int i = 0; i < originalFloatData.Length; i++)
            {
                Assert.AreEqual(originalFloatData[i], readFloatData[i], $"Data element {i} should be preserved");
            }
        }

        [Test]
        public void Spec_ValidationIntegration_ShouldCatchAllViolations()
        {
            // Test that our comprehensive validation catches all specification violations
            var violationTests = new (string description, Action<NiftiHeader> violation)[]
            {
                ("Invalid sizeof_hdr", h => h.sizeof_hdr = 300),
                ("Invalid magic number", h => h.magic = new byte[] { 0x00, 0x00, 0x00, 0x00 }),
                ("Invalid dimension count", h => h.dim[0] = 8),
                ("Negative spatial dimension", h => h.dim[1] = -1),
                ("Invalid data type/bitpix", h => { h.datatype = NiftiHeader.DT_FLOAT32; h.bitpix = 16; }),
                ("Invalid quaternion magnitude", h => { h.quatern_b = 1.0f; h.quatern_c = 1.0f; h.quatern_d = 1.0f; }),
                ("Invalid vox_offset for .nii", h => { h.magic = new byte[] { 0x6E, 0x2B, 0x31, 0x00 }; h.vox_offset = 100.0f; }),
            };

            foreach (var (description, violation) in violationTests)
            {
                var header = CreateValidHeader();
                violation(header);
                
                Assert.Throws<InvalidDataException>(() => header.ValidateHeader(),
                    $"Validation should catch: {description}");
            }
        }

        #endregion

        #region Helper Methods

        private NiftiHeader CreateValidHeader()
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
            header.pixdim[1] = 1.0f;
            header.pixdim[2] = 1.0f;
            header.pixdim[3] = 1.0f;
            return header;
        }

        private NiftiHeader CreateComprehensiveHeader()
        {
            var header = CreateValidHeader();
            
            // Set all major fields to test round-trip preservation
            header.intent_code = NiftiHeader.NIFTI_INTENT_TTEST;
            header.intent_p1 = 10.0f; // DOF for t-test
            header.intent_p2 = 0.0f;
            header.intent_p3 = 0.0f;
            
            header.qform_code = NiftiHeader.NIFTI_XFORM_SCANNER_ANAT;
            header.sform_code = NiftiHeader.NIFTI_XFORM_MNI_152;
            
            header.quatern_b = 0.0f;
            header.quatern_c = 0.0f;
            header.quatern_d = 0.0f;
            header.qoffset_x = 0.0f;
            header.qoffset_y = 0.0f;
            header.qoffset_z = 0.0f;
            
            header.srow_x = new float[] { 1.0f, 0.0f, 0.0f, 0.0f };
            header.srow_y = new float[] { 0.0f, 1.0f, 0.0f, 0.0f };
            header.srow_z = new float[] { 0.0f, 0.0f, 1.0f, 0.0f };
            
            header.scl_slope = 1.0f;
            header.scl_inter = 0.0f;
            header.cal_max = 1000.0f;
            header.cal_min = 0.0f;
            
            header.xyzt_units = NiftiHeader.SpaceTimeToXyzt(NiftiHeader.NIFTI_UNITS_MM, NiftiHeader.NIFTI_UNITS_SEC);
            
            var description = "Comprehensive test header for NIfTI-1 specification compliance";
            var descBytes = System.Text.Encoding.ASCII.GetBytes(description);
            header.descrip = new byte[80];
            Array.Copy(descBytes, header.descrip, Math.Min(descBytes.Length, 79));
            
            return header;
        }

        private float[] CreateTestData()
        {
            var data = new float[64 * 64 * 32];
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = (float)Math.Sin(i * 0.01) * 1000.0f;
            }
            return data;
        }

        private void VerifyHeaderFieldsPreserved(NiftiHeader original, NiftiHeader read)
        {
            Assert.AreEqual(original.sizeof_hdr, read.sizeof_hdr, "sizeof_hdr should be preserved");
            CollectionAssert.AreEqual(original.magic, read.magic, "magic should be preserved");
            CollectionAssert.AreEqual(original.dim, read.dim, "dim should be preserved");
            Assert.AreEqual(original.datatype, read.datatype, "datatype should be preserved");
            Assert.AreEqual(original.bitpix, read.bitpix, "bitpix should be preserved");
            CollectionAssert.AreEqual(original.pixdim, read.pixdim, "pixdim should be preserved");
            Assert.AreEqual(original.vox_offset, read.vox_offset, "vox_offset should be preserved");
            Assert.AreEqual(original.intent_code, read.intent_code, "intent_code should be preserved");
            Assert.AreEqual(original.qform_code, read.qform_code, "qform_code should be preserved");
            Assert.AreEqual(original.sform_code, read.sform_code, "sform_code should be preserved");
            Assert.AreEqual(original.xyzt_units, read.xyzt_units, "xyzt_units should be preserved");
        }

        #endregion
    }
}