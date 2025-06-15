using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Nifti.NET
{
    /// <summary>
    /// Utility class for validating NIfTI files and headers against the NIfTI-1 specification
    /// </summary>
    public static class NiftiValidator
    {
        /// <summary>
        /// Validation result containing any issues found
        /// </summary>
        public class ValidationResult
        {
            public bool IsValid { get; set; }
            public List<string> Errors { get; set; } = new List<string>();
            public List<string> Warnings { get; set; } = new List<string>();
            public List<string> Info { get; set; } = new List<string>();

            public bool HasErrors => Errors.Count > 0;
            public bool HasWarnings => Warnings.Count > 0;
            
            public override string ToString()
            {
                var result = $"Valid: {IsValid}\n";
                if (HasErrors)
                {
                    result += $"Errors ({Errors.Count}):\n";
                    result += string.Join("\n", Errors.Select(e => $"  - {e}"));
                    result += "\n";
                }
                if (HasWarnings)
                {
                    result += $"Warnings ({Warnings.Count}):\n";
                    result += string.Join("\n", Warnings.Select(w => $"  - {w}"));
                    result += "\n";
                }
                if (Info.Count > 0)
                {
                    result += $"Info ({Info.Count}):\n";
                    result += string.Join("\n", Info.Select(i => $"  - {i}"));
                }
                return result;
            }
        }

        /// <summary>
        /// Validates a NIfTI file against the NIfTI-1 specification
        /// </summary>
        /// <param name="filePath">Path to the NIfTI file</param>
        /// <returns>Validation result</returns>
        public static ValidationResult ValidateFile(string filePath)
        {
            var result = new ValidationResult();

            try
            {
                if (!File.Exists(filePath))
                {
                    result.Errors.Add($"File does not exist: {filePath}");
                    result.IsValid = false;
                    return result;
                }

                var fileInfo = new FileInfo(filePath);
                result.Info.Add($"File size: {fileInfo.Length} bytes");

                // Check file extension
                var extension = Path.GetExtension(filePath).ToLower();
                var validExtensions = new[] { ".nii", ".hdr", ".img" };
                if (!validExtensions.Contains(extension))
                {
                    result.Warnings.Add($"Unusual file extension: {extension}. Expected .nii, .hdr, or .img");
                }

                // Check for gzipped files
                if (filePath.EndsWith(".gz", StringComparison.OrdinalIgnoreCase))
                {
                    result.Info.Add("File is gzip compressed");
                    if (NiftiFile.IsCompressed(filePath))
                    {
                        result.Info.Add("Gzip compression validated");
                    }
                    else
                    {
                        result.Errors.Add("File has .gz extension but is not actually gzip compressed");
                    }
                }

                // Read and validate header
                NiftiHeader header;
                try
                {
                    header = NiftiFile.ReadHeader(filePath);
                }
                catch (Exception ex)
                {
                    result.Errors.Add($"Failed to read header: {ex.Message}");
                    result.IsValid = false;
                    return result;
                }

                // Validate header
                var headerValidation = ValidateHeader(header);
                result.Errors.AddRange(headerValidation.Errors);
                result.Warnings.AddRange(headerValidation.Warnings);
                result.Info.AddRange(headerValidation.Info);

                // Check data consistency if we can read the full file
                try
                {
                    var nifti = NiftiFile.Read(filePath);
                    var dataValidation = ValidateDataConsistency(nifti);
                    result.Errors.AddRange(dataValidation.Errors);
                    result.Warnings.AddRange(dataValidation.Warnings);
                    result.Info.AddRange(dataValidation.Info);
                }
                catch (Exception ex)
                {
                    result.Errors.Add($"Failed to read full file for data validation: {ex.Message}");
                }

                result.IsValid = !result.HasErrors;
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Unexpected error during validation: {ex.Message}");
                result.IsValid = false;
            }

            return result;
        }

        /// <summary>
        /// Validates a NIfTI header against the NIfTI-1 specification
        /// </summary>
        /// <param name="header">Header to validate</param>
        /// <returns>Validation result</returns>
        public static ValidationResult ValidateHeader(NiftiHeader header)
        {
            var result = new ValidationResult();

            try
            {
                // Use the built-in validation first
                header.ValidateHeader();
                result.Info.Add("Header passes basic NIfTI-1 validation");
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Header validation failed: {ex.Message}");
            }

            // Additional detailed checks
            PerformDetailedHeaderValidation(header, result);

            result.IsValid = !result.HasErrors;
            return result;
        }

        /// <summary>
        /// Validates data consistency within a NIfTI object
        /// </summary>
        /// <param name="nifti">NIfTI object to validate</param>
        /// <returns>Validation result</returns>
        public static ValidationResult ValidateDataConsistency(Nifti nifti)
        {
            var result = new ValidationResult();

            if (nifti?.Header == null)
            {
                result.Errors.Add("NIfTI object or header is null");
                result.IsValid = false;
                return result;
            }

            var header = nifti.Header;
            var data = nifti.Data;

            // Check data presence
            if (data == null)
            {
                result.Warnings.Add("No data present in NIfTI object");
                result.IsValid = true;
                return result;
            }

            // Check data length consistency
            var expectedVoxels = header.GetTotalVoxels();
            var actualElements = GetDataLength(data);

            if (expectedVoxels != actualElements)
            {
                result.Errors.Add($"Data length mismatch: expected {expectedVoxels} voxels, got {actualElements} elements");
            }
            else
            {
                result.Info.Add($"Data length consistent: {actualElements} elements");
            }

            // Check data type consistency
            var expectedType = GetExpectedDataType(header.datatype);
            var actualType = data.GetType().GetElementType();

            if (expectedType != null && actualType != expectedType)
            {
                result.Warnings.Add($"Data type mismatch: header indicates {expectedType.Name}, data is {actualType?.Name}");
            }
            else if (expectedType != null)
            {
                result.Info.Add($"Data type consistent: {actualType?.Name}");
            }

            // Check for NaN and infinite values in floating-point data
            if (data is float[] floatData)
            {
                var nanCount = floatData.Count(float.IsNaN);
                var infCount = floatData.Count(float.IsInfinity);
                
                if (nanCount > 0)
                {
                    result.Warnings.Add($"Data contains {nanCount} NaN values");
                }
                if (infCount > 0)
                {
                    result.Warnings.Add($"Data contains {infCount} infinite values");
                }
                
                if (nanCount == 0 && infCount == 0)
                {
                    result.Info.Add("No NaN or infinite values in float data");
                }
            }
            else if (data is double[] doubleData)
            {
                var nanCount = doubleData.Count(double.IsNaN);
                var infCount = doubleData.Count(double.IsInfinity);
                
                if (nanCount > 0)
                {
                    result.Warnings.Add($"Data contains {nanCount} NaN values");
                }
                if (infCount > 0)
                {
                    result.Warnings.Add($"Data contains {infCount} infinite values");
                }
                
                if (nanCount == 0 && infCount == 0)
                {
                    result.Info.Add("No NaN or infinite values in double data");
                }
            }

            result.IsValid = !result.HasErrors;
            return result;
        }

        /// <summary>
        /// Performs detailed header validation with comprehensive checks
        /// </summary>
        private static void PerformDetailedHeaderValidation(NiftiHeader header, ValidationResult result)
        {
            // Check for recommended practices and common issues

            // Pixel dimensions
            bool hasValidPixdim = false;
            for (int i = 1; i <= Math.Min(header.dim[0], 3); i++)
            {
                if (header.pixdim[i] > 0)
                {
                    hasValidPixdim = true;
                    break;
                }
            }
            
            if (!hasValidPixdim)
            {
                result.Warnings.Add("No positive pixel dimensions specified");
            }
            else
            {
                result.Info.Add("Pixel dimensions specified");
            }

            // Check vox_offset alignment
            if (header.IsNiiFile() && header.vox_offset % 16 != 0)
            {
                result.Warnings.Add($"vox_offset ({header.vox_offset}) is not a multiple of 16, which may cause compatibility issues");
            }

            // Check for coordinate system information
            if (header.qform_code == NiftiHeader.NIFTI_XFORM_UNKNOWN && header.sform_code == NiftiHeader.NIFTI_XFORM_UNKNOWN)
            {
                result.Warnings.Add("No coordinate system information specified (both qform_code and sform_code are UNKNOWN)");
            }
            else
            {
                result.Info.Add($"Coordinate system specified: qform_code={header.qform_code}, sform_code={header.sform_code}");
            }

            // Check scaling parameters
            if (header.scl_slope != 0.0f && header.scl_slope != 1.0f)
            {
                result.Info.Add($"Data scaling enabled: slope={header.scl_slope}, intercept={header.scl_inter}");
            }

            // Check for RGB data without scaling
            if (header.datatype == NiftiHeader.DT_RGB24 && header.scl_slope != 0.0f)
            {
                result.Warnings.Add("RGB data should not use scaling parameters (scl_slope should be 0)");
            }

            // Check intent code consistency
            if (header.intent_code != NiftiHeader.NIFTI_INTENT_NONE)
            {
                result.Info.Add($"Intent code specified: {header.intent_code}");
                
                // Check if statistical intent codes have reasonable parameters
                if (header.intent_code >= NiftiHeader.NIFTI_FIRST_STATCODE && 
                    header.intent_code <= NiftiHeader.NIFTI_LAST_STATCODE)
                {
                    result.Info.Add("Statistical intent code detected");
                }
            }

            // Check slice timing information
            if (header.slice_code != NiftiHeader.NIFTI_SLICE_UNKNOWN)
            {
                result.Info.Add($"Slice timing information present: slice_code={header.slice_code}");
                if (header.slice_duration <= 0)
                {
                    result.Warnings.Add("Slice timing code specified but slice_duration is not positive");
                }
            }

            // Check for common dimension patterns
            if (header.dim[0] >= 3)
            {
                var isIsotropic = Math.Abs(header.pixdim[1] - header.pixdim[2]) < 0.001f && 
                                 Math.Abs(header.pixdim[1] - header.pixdim[3]) < 0.001f;
                if (isIsotropic && header.pixdim[1] > 0)
                {
                    result.Info.Add($"Isotropic voxels: {header.pixdim[1]}mm");
                }
                else if (header.pixdim[1] > 0)
                {
                    result.Info.Add($"Anisotropic voxels: {header.pixdim[1]}×{header.pixdim[2]}×{header.pixdim[3]}mm");
                }
            }

            // Check for 4D time series
            if (header.dim[0] >= 4 && header.dim[4] > 1)
            {
                result.Info.Add($"4D time series data: {header.dim[4]} time points");
                if (header.pixdim[4] > 0)
                {
                    result.Info.Add($"Repetition time (TR): {header.pixdim[4]} seconds");
                }
            }

            // Check for 5D+ data
            if (header.dim[0] >= 5 && header.dim[5] > 1)
            {
                result.Info.Add($"5D+ data detected: dim[5]={header.dim[5]}");
            }

            // Check description field
            if (header.descrip != null)
            {
                var description = System.Text.Encoding.ASCII.GetString(header.descrip).TrimEnd('\0');
                if (!string.IsNullOrEmpty(description))
                {
                    result.Info.Add($"Description: {description}");
                }
            }
        }

        /// <summary>
        /// Gets the length of the data array regardless of its type
        /// </summary>
        private static long GetDataLength(dynamic data)
        {
            if (data == null) return 0;
            
            if (data is Array array)
            {
                return array.Length;
            }
            
            // Fallback for other types
            try
            {
                return data.Length;
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Gets the expected .NET type for a given NIfTI data type
        /// </summary>
        private static Type GetExpectedDataType(short niftiDataType)
        {
            switch (niftiDataType)
            {
                case NiftiHeader.DT_UINT8:
                    return typeof(byte);
                case NiftiHeader.DT_INT8:
                    return typeof(sbyte);
                case NiftiHeader.DT_INT16:
                    return typeof(short);
                case NiftiHeader.DT_UINT16:
                    return typeof(ushort);
                case NiftiHeader.DT_INT32:
                    return typeof(int);
                case NiftiHeader.DT_UINT32:
                    return typeof(uint);
                case NiftiHeader.DT_INT64:
                    return typeof(long);
                case NiftiHeader.DT_UINT64:
                    return typeof(ulong);
                case NiftiHeader.DT_FLOAT32:
                    return typeof(float);
                case NiftiHeader.DT_FLOAT64:
                    return typeof(double);
                default:
                    return null; // Unknown or complex types
            }
        }
    }
}