using Nifti.NET;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Tests
{
    public class NiftiTests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void Index()
        {
            string fileLR = Path.Combine("resources", "avg152T1_LR_nifti.nii");
            var niiLR = NiftiFile.Read(fileLR);

            var pos = 0;
            for (int k = 0; k < niiLR.Dimensions[2]; ++k)
            {
                for (int j = 0; j < niiLR.Dimensions[1]; ++j)
                {
                    for (int i = 0; i < niiLR.Dimensions[0]; ++i)
                    {
                        var val = niiLR[i, j, k];
                        pos++;
                    }
                }

            }

            // TODO: Add more exhastive checks.
            Assert.IsTrue(pos == niiLR.Data.Length);
        }
    }
}
