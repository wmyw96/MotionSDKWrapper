using System;
using System.Text;
using System.Runtime.InteropServices;
using Emgu.CV;
using System.IO;
using st_result_t = System.Int32;

namespace SDK_Wrapper
{
    [StructLayout(LayoutKind.Sequential)]
    public struct st_pointf_t
    {
        public float x;
        public float y;
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct st_motion_body_t
    {

        public IntPtr keypoints;    ///< 人体检测到的关键点
        public IntPtr keypoint_scores;    ///< 关键点置信度

        public int keypoints_count;       ///< 关键点数量
    }
    public enum st_pixel_format
    {
        ST_PIX_FMT_GRAY8,   ///< Y    1       8bpp ( 单通道8bit灰度像素 )
        ST_PIX_FMT_YUV420P, ///< YUV  4:2:0   12bpp ( 3通道, 一个亮度通道, 另两个为U分量和V分量通道, 所有通道都是连续的 )
        ST_PIX_FMT_NV12,    ///< YUV  4:2:0   12bpp ( 2通道, 一个通道是连续的亮度通道, 另一通道为UV分量交错 )
        ST_PIX_FMT_NV21,    ///< YUV  4:2:0   12bpp ( 2通道, 一个通道是连续的亮度通道, 另一通道为VU分量交错 )
        ST_PIX_FMT_BGRA8888,    ///< BGRA 8:8:8:8 32bpp ( 4通道32bit BGRA 像素 )
        ST_PIX_FMT_BGR888	///< BGR  8:8:8   24bpp ( 3通道24bit BGR 像素 )
    }
    public enum st_motion_orientation
    {
        ST_MOTION_UP = 0x0000001,       ///< 向上
        ST_MOTION_LEFT = 0x0000002,     ///< 向左
        ST_MOTION_DOWN = 0x0000004,     ///< 向下
        ST_MOTION_RIGHT = 0x0000008,		///< 向右
    }


    public static class Motion_Track
    {
        public static string license_file_path = "../../../license/";
        public static byte[] p_model_path = Encoding.Default.GetBytes("../../../Plugins/Body_Track.model");
        [DllImport("stmotion_api.dll")]
        public extern static st_result_t st_motion_public_init_license(byte[] license_file_path, byte[] license_repo);
        [DllImport("stmotion_api.dll")]
        public extern static st_result_t st_motion_body_create_tracker(ref IntPtr p_handle, byte[] p_model_path, uint config);
        [DllImport("stmotion_api.dll")]
        public extern static st_result_t st_motion_body_set_track_body_cnt_limit(IntPtr p_handle, int body_count_limit, int[] p_value);
        [DllImport("stmotion_api.dll")]
        public extern static st_result_t st_motion_body_track(IntPtr p_handle,
                                                                        IntPtr image_data,
                                                                        st_pixel_format pixel_format,
                                                                        int image_width,
                                                                        int image_height,
                                                                        st_motion_orientation orientation,
                                                                        ref IntPtr p_bodies_array,
                                                                        ref int p_bodies_count
                                                                        );

        [DllImport("stmotion_api.dll")]
        public extern static void st_motion_body_release_track_result(IntPtr p_bodies_array, int bodies_count);
        [DllImport("stmotion_api.dll")]
        public extern static void st_motion_body_destroy_tracker(IntPtr track_handle);

        // cast intptr to managed datatype
        public static void Recon_keypoints(st_motion_body_t input, out st_pointf_t[] keypoints, out float[] keypoint_scores)
        {
            //st_motion_body_t input = (st_motion_body_t)Marshal.PtrToStructure(inputptr, typeof(st_pointf_t));
            var sizeInBytes = Marshal.SizeOf(typeof(st_pointf_t));
            keypoints = new st_pointf_t[input.keypoints_count];
            keypoint_scores = new float[input.keypoints_count];
            for (int i = 0; i < input.keypoints_count; i++)
            {
                IntPtr p = new IntPtr((input.keypoints.ToInt64() + i * sizeInBytes));
                keypoints[i] = (st_pointf_t)Marshal.PtrToStructure(p, typeof(st_pointf_t));
            }
            Marshal.Copy(input.keypoint_scores, keypoint_scores, 0, input.keypoints_count);
        }

        // cast intptr to st_motion_body_t[]
        public static void Recon_Body_T(IntPtr ptr, out st_motion_body_t[] p_bodies, int cnt)
        {
            p_bodies = null;
            var sizeInBytes = Marshal.SizeOf(typeof(st_motion_body_t));
            p_bodies = new st_motion_body_t[cnt];
            for (int i = 0; i < cnt; i++)
            {
                IntPtr p = new IntPtr((ptr.ToInt64() + i * sizeInBytes));
                p_bodies[i] = (st_motion_body_t)Marshal.PtrToStructure(p, typeof(st_motion_body_t));
            }
        }
    }

    public class SDK
    // currently only support one actor
    {
        // body handle 句柄
        IntPtr body_handle = IntPtr.Zero;
        // 函数返回状态
        const st_result_t ST_OK = 0;
        st_result_t ret;

        // 身体关键点数组
        IntPtr p_bodies = IntPtr.Zero;
        int bodies_count = 0;


        public SDK(string license_dir = null, int bodies_count = 1)
        {
            string[] files;

            if (license_dir == null)
            {
                //use default path
                files = Directory.GetFiles(Motion_Track.license_file_path, "*.lic");
            }
            else
            {
                files = Directory.GetFiles(license_dir, "*.lic");
            }
            byte[] license_path = Encoding.Default.GetBytes(files[0]);
            SDK_Initiate(license_path, bodies_count);
        }

        ~SDK()
        {
            Close();
        }

        private int SDK_Initiate(byte[] license_path, int bodies_count)
        {
            // initialize license
            ret = Motion_Track.st_motion_public_init_license(license_path, Encoding.Default.GetBytes("license"));
            if (ret != ST_OK) return ret;

            ret = Motion_Track.st_motion_body_create_tracker(ref body_handle, Motion_Track.p_model_path, 0);
            if (ret != ST_OK)
            {
                Motion_Track.st_motion_body_destroy_tracker(body_handle);
                return ret;
            }

            // limit the track bodies count, -1 means no limit, only support 1 actor here
            ret = Motion_Track.st_motion_body_set_track_body_cnt_limit(body_handle, bodies_count, null);
            if (ret != ST_OK) return ret;
            return 0;
        }

        public int ProcessFrame(Mat buffer, out st_pointf_t[][] keypoints, out float[][] keypoint_scores)
        {
            keypoints = null;
            keypoint_scores = null;

            // 初始化指针
            p_bodies = IntPtr.Zero;
            bodies_count = 0;
            // body track
            ret = Motion_Track.st_motion_body_track(body_handle,
                buffer.DataPointer,
                //frame,
                st_pixel_format.ST_PIX_FMT_BGR888,
                buffer.Width, buffer.Height,
                st_motion_orientation.ST_MOTION_UP,
                ref p_bodies,
                ref bodies_count);
            if (ret != ST_OK) return ret;

            Motion_Track.Recon_Body_T(p_bodies, out st_motion_body_t[] p_bodies_m, bodies_count);
            keypoints = new st_pointf_t[bodies_count][];
            keypoint_scores = new float[bodies_count][];
            for (int i = 0; i < bodies_count; i++)
            {
                Motion_Track.Recon_keypoints(p_bodies_m[i], out keypoints[i], out keypoint_scores[i]);

            }
            // release memory
            Motion_Track.st_motion_body_release_track_result(p_bodies, bodies_count);
            return 0;
        }

        private void Close()
        {
            Motion_Track.st_motion_body_destroy_tracker(p_bodies);
        }

    }
}
