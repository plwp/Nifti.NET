using System;
using System.IO;
using Nifti.NET;

namespace Nifti.NET.Examples
{
    /// <summary>
    /// Example demonstrating how to create NIfTI objects from data arrays
    /// </summary>
    public class CreateFromDataExample
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Nifti.NET - Create From Data Example");
            Console.WriteLine("====================================");

            // Example 1: Create a simple 3D volume from float data
            Console.WriteLine("\n1. Creating 3D volume from float array...");
            Create3DVolumeExample();

            // Example 2: Create a 2D image with custom pixel dimensions
            Console.WriteLine("\n2. Creating 2D image with custom pixel dimensions...");
            Create2DImageExample();

            // Example 3: Create from integer data
            Console.WriteLine("\n3. Creating volume from integer data...");
            CreateFromIntegerDataExample();

            // Example 4: Create 4D time series data
            Console.WriteLine("\n4. Creating 4D time series data...");
            Create4DTimeSeriesExample();

            Console.WriteLine("\nAll examples completed successfully!");
        }

        static void Create3DVolumeExample()
        {
            // Create a 3D volume: 64x64x32 voxels
            int width = 64, height = 64, depth = 32;
            var data = new float[width * height * depth];

            // Fill with some test pattern (a sphere in the center)
            int centerX = width / 2, centerY = height / 2, centerZ = depth / 2;
            float radius = 15.0f;

            for (int z = 0; z < depth; z++)
            {
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        int index = x + y * width + z * width * height;
                        float distance = (float)Math.Sqrt(
                            Math.Pow(x - centerX, 2) + 
                            Math.Pow(y - centerY, 2) + 
                            Math.Pow(z - centerZ, 2));
                        
                        // Create a sphere with intensity based on distance from center
                        data[index] = distance <= radius ? 1000.0f - (distance / radius) * 500.0f : 0.0f;
                    }
                }
            }

            // Create NIfTI object
            var dimensions = new int[] { width, height, depth };
            var pixelDimensions = new float[] { 1.0f, 1.0f, 2.0f }; // 1mm x 1mm x 2mm voxels
            var nifti = Nifti.CreateFromData(data, dimensions, pixelDimensions, "Example 3D sphere volume");

            Console.WriteLine($"  Created 3D volume: {nifti.Header.dim[1]}x{nifti.Header.dim[2]}x{nifti.Header.dim[3]}");
            Console.WriteLine($"  Data type: {nifti.Header.datatype} ({nifti.Header.bitpix} bits per pixel)");
            Console.WriteLine($"  Voxel size: {nifti.Header.pixdim[1]}x{nifti.Header.pixdim[2]}x{nifti.Header.pixdim[3]} mm");

            // Access some voxel values
            Console.WriteLine($"  Center voxel value: {nifti[centerX, centerY, centerZ]:F1}");
            Console.WriteLine($"  Corner voxel value: {nifti[0, 0, 0]:F1}");

            // Save the file
            NiftiFile.Write(nifti, "example_3d_sphere.nii");
            Console.WriteLine("  Saved as: example_3d_sphere.nii");
        }

        static void Create2DImageExample()
        {
            // Create a 2D image: 256x256 pixels
            int width = 256, height = 256;
            var data = new byte[width * height];

            // Create a checkerboard pattern
            int checkSize = 32;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = x + y * width;
                    bool isWhite = ((x / checkSize) + (y / checkSize)) % 2 == 0;
                    data[index] = (byte)(isWhite ? 255 : 0);
                }
            }

            // Create NIfTI object with high-resolution pixel dimensions
            var dimensions = new int[] { width, height };
            var pixelDimensions = new float[] { 0.5f, 0.5f }; // 0.5mm x 0.5mm pixels
            var nifti = Nifti.CreateFromData(data, dimensions, pixelDimensions, "Checkerboard pattern image");

            Console.WriteLine($"  Created 2D image: {nifti.Header.dim[1]}x{nifti.Header.dim[2]}");
            Console.WriteLine($"  Data type: {nifti.Header.datatype} ({nifti.Header.bitpix} bits per pixel)");
            Console.WriteLine($"  Pixel size: {nifti.Header.pixdim[1]}x{nifti.Header.pixdim[2]} mm");

            // Save the file
            NiftiFile.Write(nifti, "example_2d_checkerboard.nii");
            Console.WriteLine("  Saved as: example_2d_checkerboard.nii");
        }

        static void CreateFromIntegerDataExample()
        {
            // Create labeled regions (typical for segmentation masks)
            int width = 128, height = 128, depth = 64;
            var data = new short[width * height * depth];

            // Create different labeled regions
            for (int z = 0; z < depth; z++)
            {
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        int index = x + y * width + z * width * height;
                        
                        // Assign labels based on position
                        if (x < width / 3) data[index] = 1;      // Left region
                        else if (x < 2 * width / 3) data[index] = 2;  // Middle region
                        else data[index] = 3;                    // Right region
                    }
                }
            }

            var dimensions = new int[] { width, height, depth };
            var nifti = Nifti.CreateFromData(data, dimensions, description: "Labeled regions example");

            Console.WriteLine($"  Created labeled volume: {nifti.Header.dim[1]}x{nifti.Header.dim[2]}x{nifti.Header.dim[3]}");
            Console.WriteLine($"  Data type: {nifti.Header.datatype} ({nifti.Header.bitpix} bits per pixel)");
            Console.WriteLine($"  Labels: {nifti[10, 64, 32]} (left), {nifti[64, 64, 32]} (middle), {nifti[118, 64, 32]} (right)");

            // Save the file
            NiftiFile.Write(nifti, "example_labeled_regions.nii");
            Console.WriteLine("  Saved as: example_labeled_regions.nii");
        }

        static void Create4DTimeSeriesExample()
        {
            // Create a 4D time series: 32x32x16 voxels x 100 time points
            int width = 32, height = 32, depth = 16, timePoints = 100;
            var data = new float[width * height * depth * timePoints];

            // Create synthetic time series data (sinusoidal activation)
            var random = new Random(42);
            for (int t = 0; t < timePoints; t++)
            {
                for (int z = 0; z < depth; z++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            int index = x + y * width + z * width * height + t * width * height * depth;
                            
                            // Add some baseline noise
                            float noise = (float)(random.NextDouble() - 0.5) * 10.0f;
                            
                            // Add sinusoidal signal in center region
                            float distanceFromCenter = (float)Math.Sqrt(
                                Math.Pow(x - width/2.0, 2) + 
                                Math.Pow(y - height/2.0, 2));
                            
                            if (distanceFromCenter < 8.0f)
                            {
                                float signal = 100.0f + 50.0f * (float)Math.Sin(2 * Math.PI * t / 20.0); // 20-point cycle
                                data[index] = signal + noise;
                            }
                            else
                            {
                                data[index] = 100.0f + noise; // Baseline + noise
                            }
                        }
                    }
                }
            }

            var dimensions = new int[] { width, height, depth, timePoints };
            var pixelDimensions = new float[] { 2.0f, 2.0f, 3.0f, 1.5f }; // 2x2x3mm voxels, 1.5s repetition time
            var nifti = Nifti.CreateFromData(data, dimensions, pixelDimensions, "Synthetic fMRI time series");

            Console.WriteLine($"  Created 4D time series: {nifti.Header.dim[1]}x{nifti.Header.dim[2]}x{nifti.Header.dim[3]}x{nifti.Header.dim[4]}");
            Console.WriteLine($"  Data type: {nifti.Header.datatype} ({nifti.Header.bitpix} bits per pixel)");
            Console.WriteLine($"  Voxel size: {nifti.Header.pixdim[1]}x{nifti.Header.pixdim[2]}x{nifti.Header.pixdim[3]} mm");
            Console.WriteLine($"  TR (repetition time): {nifti.Header.pixdim[4]} seconds");

            // Access time series at center voxel
            Console.WriteLine($"  Center voxel time series (first 5 points): ");
            for (int t = 0; t < 5; t++)
            {
                Console.WriteLine($"    t={t}: {nifti[width/2, height/2, depth/2, t]:F1}");
            }

            // Save the file
            NiftiFile.Write(nifti, "example_4d_timeseries.nii");
            Console.WriteLine("  Saved as: example_4d_timeseries.nii");
        }
    }
}