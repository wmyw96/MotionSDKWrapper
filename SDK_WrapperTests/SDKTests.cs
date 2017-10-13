using Microsoft.VisualStudio.TestTools.UnitTesting;
using Emgu.CV;
using SDK_Wrapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SDK_Wrapper.Tests
{
    [TestClass()]
    public class SDKTests
    {
        [TestMethod()]
        public void SDKTest()
        {
            SDK SDK = new SDK();
        }

        [TestMethod()]
        public void ProcessFrameTest()
        {
            SDK SDK = new SDK(bodies_count:-1);
            Mat img = CvInvoke.Imread("test.jpg");
            for (int i = 0; i < 30; i++)
            {
                int ret = SDK.ProcessFrame(img, out st_pointf_t[][] key_points, out float[][] keypoints_conf);
                for (int j = 0; j < key_points.GetLength(0); j++)
                {
                    for (int k = 0; k < key_points[j].GetLength(0); k++)
                    {
                        Console.WriteLine("body:{0}, x:{1}, y:{2}", j + 1, key_points[j][k].x, key_points[j][k].y);
                    }
                }
                Assert.IsTrue(ret == 0);
            }
        }
    }
}