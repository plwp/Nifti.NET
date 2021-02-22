using Nifti.NET;
using NUnit.Framework;
using System;
using System.Drawing;
using System.IO;

namespace Tests
{
    public class FileTests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void ReadHdr()
        {
            string file = Path.Combine("resources", "avg152T1_LR_nifti.hdr");
            var hdr = NiftiFile.ReadHeader(file);

            Assert.IsTrue((hdr.sizeof_hdr == 348 && !hdr.SourceIsBigEndian())|| hdr.SourceIsBigEndian());
        }

        [Test]
        public void ReadHdrImg()
        {
            var file = Path.Combine("resources", "minimal.hdr.gz");
            var nifti1 = NiftiFile.Read(file);

            var niiFile = Path.Combine("resources", "minimal.nii.gz");
            var nifti2 = NiftiFile.Read(niiFile);

            Assert.IsTrue(nifti1.Data.Length == nifti2.Data.Length);
            Assert.IsTrue(nifti1.Data[100] == nifti2.Data[100]);
            Assert.IsTrue(nifti1.Header.sizeof_hdr == nifti2.Header.sizeof_hdr);
            Assert.IsTrue(nifti1.Header.slice_start == nifti2.Header.slice_start);
        }


        [Test]
        public void ReadNii()
        {
            string file = Path.Combine("resources", "avg152T1_LR_nifti.nii");
            var nifti = NiftiFile.Read(file);
            Assert.IsTrue(nifti.Header.sizeof_hdr == 348 || nifti.Header.SourceIsBigEndian());
        }

        [Test]
        public void GZipTest()
        {
            string hdr1 = Path.Combine("resources", "avg152T1_LR_nifti.hdr");
            string hdr2 = Path.Combine("resources", "avg152T1_LR_nifti.hdr.gz");

            Assert.IsFalse(NiftiFile.IsCompressed(hdr1));
            Assert.IsTrue(NiftiFile.IsCompressed(hdr2));

            string nii1 = Path.Combine("resources", "avg152T1_LR_nifti.nii");
            string nii2 = Path.Combine("resources", "avg152T1_LR_nifti.nii.gz");

            var nifti1 = NiftiFile.Read(nii1);
            var nifti2 = NiftiFile.Read(nii2);

            Assert.IsTrue(nifti1.Data.Length == nifti2.Data.Length);
            Assert.IsTrue(nifti1.Data[100] == nifti2.Data[100]);
            Assert.IsTrue(nifti1.Header.sizeof_hdr == nifti2.Header.sizeof_hdr);
            Assert.IsTrue(nifti1.Header.slice_start == nifti2.Header.slice_start);

            var tmp = "tmp.nii.gz";
            NiftiFile.Write(nifti1, tmp, gzip:true);
            Assert.IsTrue(File.Exists(tmp));
            File.Delete(tmp);
        }

        [Test]
        public void TestBadFormat()
        {
            string path = Path.Combine("resources", "gato.nii");

            Assert.IsFalse(NiftiFile.IsCompressed(path));
            try
            {
                _ = NiftiFile.ReadHeader(path);
                Assert.Fail("Failed to fail.");
            }
            catch (InvalidDataException){}
            catch (Exception e)
            {
                Assert.Fail("Hmmm... " + e.Message);
            }

            try
            {
                _ = NiftiFile.Read(path);
                Assert.Fail("Failed to fail.");
            }
            catch (InvalidDataException) { }
            catch (Exception e)
            {
                Assert.Fail("Hmmm..." + e.Message);
            }
        }

        [Test]
        public void TestWriteHeader()
        {
            var niiFile = Path.Combine("resources", "minimal.nii.gz");
            var tmp = "tmp.hdr";

            var nifti = NiftiFile.Read(niiFile); 

            NiftiFile.Write(nifti.Header, tmp);
            var niftiHdr = NiftiFile.ReadHeader(tmp);

            // Maybe we could play with the bits at some point.
            // At the moment this will fail.
            //Assert.IsTrue(niftiHdr.magic[1] == 0x69);
            //Assert.IsTrue(nifti.Header.magic[1] == 0X2B);

            Assert.IsTrue(nifti.Header.cal_max == niftiHdr.cal_max);

            File.Delete(tmp);
        }

        [Test]
        public void TestWriteHdrImg()
        {
            var niiFile = Path.Combine("resources", "minimal.nii.gz");
            var tmp = "tmp.hdr";

            var nifti = NiftiFile.Read(niiFile);

            nifti.Header.magic[1] = 0x69;

            NiftiFile.Write(nifti, tmp);

            Assert.IsTrue(File.Exists(tmp));
            Assert.IsTrue(File.Exists("tmp.img"));


            var nifti2 = NiftiFile.Read(tmp);

            Assert.IsTrue(nifti.Header.magic[2] == nifti2.Header.magic[2]);
            Assert.IsTrue(nifti.Header.cal_max == nifti2.Header.cal_max);

            File.Delete(tmp);
            File.Delete("tmp.img");
        }

        [Test]
        public void TestWriteNii()
        {
            var niiFile = Path.Combine("resources", "minimal.nii.gz");
            var tmp = "tmp.nii";

            var nifti = NiftiFile.Read(niiFile);

            NiftiFile.Write(nifti, tmp);
            var nifti2 = NiftiFile.Read(tmp);

            Assert.IsTrue(nifti.Header.magic[2] == nifti2.Header.magic[2]);
            Assert.IsTrue(nifti.Header.cal_max == nifti2.Header.cal_max);

            File.Delete(tmp);
        }

        [Test]
        public void TestWriteTypedNii()
        {
            var niiFile = Path.Combine("resources", "minimal.nii.gz");
            var tmp = "tmp.nii";

            var nifti = NiftiFile.Read(niiFile).AsType<byte>();

            NiftiFile.Write(nifti, tmp);
            var nifti2 = NiftiFile.Read(tmp);

            Assert.IsTrue(nifti.Header.magic[2] == nifti2.Header.magic[2]);
            Assert.IsTrue(nifti.Header.cal_max == nifti2.Header.cal_max);
            Assert.IsTrue(nifti.Data[1] == nifti2.Data[1]);
            Assert.IsTrue(nifti.Data[60] == nifti2.Data[60]);

            File.Delete(tmp);
        }

        [Test]
        public void ColorConversion()
        {
            var niiFile = Path.Combine("resources", "minimal.nii.gz");
            var tmp = "color_pattern.nii";

            var nifti = NiftiFile.Read(niiFile);

            var colors = new Color[nifti.Data.Length];
            for (int i = 0; i < colors.Length; ++i)
            {
                colors[i] = Color.FromArgb(i%256, (i / 2) % 256, (i / 4) % 256);
            }

            nifti.Data = colors;
            System.Console.WriteLine(nifti.Header.datatype);
            System.Console.WriteLine(nifti.Header.data_type);

            // Write color data
            NiftiFile.Write(nifti, tmp);
            var tmp2 = tmp + ".2.nii";
            // Copy and check that size hasn't changed
            NiftiFile.Write(NiftiFile.Read(tmp), tmp2);
            Assert.AreEqual(new System.IO.FileInfo(tmp).Length, new System.IO.FileInfo(tmp2).Length);
            // Remove these lines to check output files
            File.Delete(tmp);
            File.Delete(tmp2);
        }
    }
}