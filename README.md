# Nifti.NET
A basic library for reading, writing and manipulating NIfTI files.

(If you're looking for the TensorFlow CNN platform, try NiftyNet (https://niftynet.io/))

## Features

- Read and write NIfTI files (.nii, .hdr/.img)
- Support for gzipped files (.nii.gz, .hdr.gz/.img.gz)
- Create NIfTI objects from data arrays
- Type-safe access to neuroimaging data
- Support for all standard NIfTI data types

## Usage

### Reading NIfTI Files

```csharp
using Nifti.NET;

// Read a NIfTI file
var nifti = NiftiFile.Read("brain_scan.nii");

// Access header information
Console.WriteLine($"Dimensions: {nifti.Header.dim[1]}x{nifti.Header.dim[2]}x{nifti.Header.dim[3]}");
Console.WriteLine($"Data type: {nifti.Header.datatype}");

// Access voxel data
float voxelValue = nifti[64, 64, 32]; // Access voxel at coordinates (64,64,32)
```

### Creating NIfTI Objects from Data

```csharp
// Create a 3D volume from a float array
var data = new float[64 * 64 * 32]; // Your data here
var dimensions = new int[] { 64, 64, 32 };
var pixelDimensions = new float[] { 1.0f, 1.0f, 2.0f }; // 1x1x2mm voxels

var nifti = Nifti.CreateFromData(data, dimensions, pixelDimensions, "My volume");

// Save to file
NiftiFile.Write(nifti, "my_volume.nii");
```

### Type-Safe Access

```csharp
// Create strongly-typed NIfTI object
var typedNifti = Nifti.CreateFromData<float>(data, dimensions);

// Type-safe data access
float[] voxelData = typedNifti.Data;
float voxelValue = typedNifti[x, y, z];
```

### Working with Different Data Types

```csharp
// Byte data (8-bit)
var byteData = new byte[256 * 256];
var byteNifti = Nifti.CreateFromData(byteData, new int[] { 256, 256 });

// Integer data (32-bit)
var intData = new int[128 * 128 * 64];
var intNifti = Nifti.CreateFromData(intData, new int[] { 128, 128, 64 });

// Double precision data (64-bit)
var doubleData = new double[64 * 64 * 32];
var doubleNifti = Nifti.CreateFromData(doubleData, new int[] { 64, 64, 32 });
```

### 4D Time Series Data

```csharp
// Create 4D fMRI time series: 64x64x32 voxels x 200 time points
var timeSeriesData = new float[64 * 64 * 32 * 200];
var dimensions = new int[] { 64, 64, 32, 200 };
var pixelDimensions = new float[] { 2.0f, 2.0f, 3.0f, 2.0f }; // TR = 2 seconds

var fmriNifti = Nifti.CreateFromData(timeSeriesData, dimensions, pixelDimensions, "fMRI time series");

// Access time series at specific voxel
float valueAtTime10 = fmriNifti[32, 32, 16, 10];
```
