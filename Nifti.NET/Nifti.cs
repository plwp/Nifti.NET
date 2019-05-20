using System;

namespace Nifti.NET
{
    public class Nifti
    {
        public NiftiHeader Header { get; set; }
        public dynamic Data { get; set; }
    }
}
