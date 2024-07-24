using OpenCVForUnity.CoreModule;
using OpenCVForUnity.DnnModule;
using OpenCVForUnity.ImgprocModule;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using OpenCVForUnity.UtilsModule;
using UnityEngine;
using OpenCVRect = OpenCVForUnity.CoreModule.Rect;

namespace YOLOv5WithOpenCVForUnity
{

    public class YOLOv5ClassPredictor
    {
        Size input_size;
        int backend;
        int target;

        Scalar IMAGENET_MEAN = new Scalar(0.485, 0.456, 0.406);// RGB mean
        Scalar IMAGENET_STD = new Scalar(0.229, 0.224, 0.225);// RGB standard deviation

        Net classification_net;
        List<string> classNames;

        List<Scalar> palette;

        Mat input_sizeMat;

        Mat getDataMat;

        public YOLOv5ClassPredictor(string modelFilepath, string classesFilepath, Size inputSize, int backend = Dnn.DNN_BACKEND_OPENCV, int target = Dnn.DNN_TARGET_CPU)
        {
            // initialize
            if (!string.IsNullOrEmpty(modelFilepath))
            {
                classification_net = Dnn.readNet(modelFilepath);
            }

            if (!string.IsNullOrEmpty(classesFilepath))
            {
                classNames = readClassNames(classesFilepath);
            }

            input_size = new Size(inputSize.width > 0 ? inputSize.width : 224, inputSize.height > 0 ? inputSize.height : 224);
            this.backend = backend;
            this.target = target;

            classification_net.setPreferableBackend(this.backend);
            classification_net.setPreferableTarget(this.target);

            palette = new List<Scalar>();
            palette.Add(new Scalar(255, 56, 56, 255));
            palette.Add(new Scalar(255, 157, 151, 255));
            palette.Add(new Scalar(255, 112, 31, 255));
            palette.Add(new Scalar(255, 178, 29, 255));
            palette.Add(new Scalar(207, 210, 49, 255));
            palette.Add(new Scalar(72, 249, 10, 255));
            palette.Add(new Scalar(146, 204, 23, 255));
            palette.Add(new Scalar(61, 219, 134, 255));
            palette.Add(new Scalar(26, 147, 52, 255));
            palette.Add(new Scalar(0, 212, 187, 255));
            palette.Add(new Scalar(44, 153, 168, 255));
            palette.Add(new Scalar(0, 194, 255, 255));
            palette.Add(new Scalar(52, 69, 147, 255));
            palette.Add(new Scalar(100, 115, 255, 255));
            palette.Add(new Scalar(0, 24, 236, 255));
            palette.Add(new Scalar(132, 56, 255, 255));
            palette.Add(new Scalar(82, 0, 133, 255));
            palette.Add(new Scalar(203, 56, 255, 255));
            palette.Add(new Scalar(255, 149, 200, 255));
            palette.Add(new Scalar(255, 55, 199, 255));
        }

        protected virtual Mat preprocess(Mat image)
        {
            // https://github.com/ultralytics/yolov5/blob/703d37ef7991387d4bf0ebc011abf8d8f2233e1d/utils/augmentations.py#L343

            // Resizes and crops the center of the image using a letterbox method.
            int c = image.channels();
            int h = (int)input_size.height;
            int w = (int)input_size.width;

            if (input_sizeMat == null)
                input_sizeMat = new Mat(h, w, CvType.CV_8UC3);// [h, w]

            int imh = image.height();
            int imw = image.width();
            int m = Mathf.Min(imh, imw);
            int top = (int)((imh - m) / 2f);
            int left = (int)((imw - m) / 2f);
            Mat image_crop = new Mat(image, new OpenCVRect(0, 0, image.width(), image.height()).intersect(new OpenCVRect(left, top, m, m)));
            Imgproc.resize(image_crop, input_sizeMat, new Size(w, h));

            Mat blob = Dnn.blobFromImage(input_sizeMat, 1.0 / 255.0, input_size, Scalar.all(0), true, false, CvType.CV_32F); // HWC to NCHW, BGR to RGB

            Mat blob_cxhxw = blob.reshape(1, new int[] { c, h, w });// [c, h, w]

            for (int i = 0; i < c; ++i)
            {
                Mat blob_1xhw = blob_cxhxw.row(i).reshape(1, 1);// [3, 224, 224] => [1, 50176]

                // Subtract blob by mean.
                Core.subtract(blob_1xhw, new Scalar(IMAGENET_MEAN.val[i]), blob_1xhw);
                // Divide blob by std.
                Core.divide(blob_1xhw, new Scalar(IMAGENET_STD.val[i]), blob_1xhw);
            }

            return blob;// [1, 3, h, w]
        }

        public virtual Mat infer(Mat image)
        {
            // cheack
            if (image.channels() != 3)
            {
                Debug.Log("The input image must be in BGR format.");
                return new Mat();
            }

            // Preprocess
            Mat input_blob = preprocess(image);

            // Forward
            classification_net.setInput(input_blob);

            List<Mat> output_blob = new List<Mat>();
            classification_net.forward(output_blob, classification_net.getUnconnectedOutLayersNames());

            // Postprocess
            Mat results = postprocess(output_blob, image.size());

            input_blob.Dispose();
            for (int i = 0; i < output_blob.Count; i++)
            {
                output_blob[i].Dispose();
            }

            return results;
        }

        protected virtual Mat postprocess(List<Mat> output_blob, Size original_shape)
        {
            Mat output_blob_0 = output_blob[0];

            Mat results = softmax(output_blob_0);

            return results;// [1, num_classes]
        }

        protected virtual Mat softmax(Mat src)
        {
            Mat dst = src.clone();

            Core.MinMaxLocResult result = Core.minMaxLoc(src);
            Scalar max = new Scalar(result.maxVal);
            Core.subtract(src, max, dst);
            Core.exp(dst, dst);
            Scalar sum = Core.sumElems(dst);
            Core.divide(dst, sum, dst);

            return dst;
        }

        public virtual void visualize(Mat image, Mat results, bool print_results = false, bool isRGB = false)
        {
            if (image.IsDisposed)
                return;

            if (results.empty())
                return;

            StringBuilder sb = null;

            if (print_results)
                sb = new StringBuilder(64);

            ClassificationData bmData = getBestMatchData(results);
            int classId = (int)bmData.cls;
            string label = getClassLabel(bmData.cls) + ", " + bmData.conf.ToString("F2");

            Scalar c = palette[classId % palette.Count];
            Scalar color = isRGB ? c : new Scalar(c.val[2], c.val[1], c.val[0], c.val[3]);

            int[] baseLine = new int[1];
            Size labelSize = Imgproc.getTextSize(label, Imgproc.FONT_HERSHEY_SIMPLEX, 1.0, 1, baseLine);

            float top = 20f + (float)labelSize.height;
            float left = (float)(image.width() / 2 - labelSize.width / 2f);

            top = Mathf.Max((float)top, (float)labelSize.height);
            Imgproc.rectangle(image, new Point(left, top - labelSize.height),
                new Point(left + labelSize.width, top + baseLine[0]), color, Core.FILLED);
            Imgproc.putText(image, label, new Point(left, top), Imgproc.FONT_HERSHEY_SIMPLEX, 1.0, Scalar.all(255), 1, Imgproc.LINE_AA);

            // Print results
            if (print_results)
            {
                sb.AppendLine("Best match: " + getClassLabel(bmData.cls) + ", " + bmData.ToString());
            }

            if (print_results)
                Debug.Log(sb.ToString());
        }

        public virtual void dispose()
        {
            if (classification_net != null)
                classification_net.Dispose();

            if (input_sizeMat != null)
                input_sizeMat.Dispose();

            input_sizeMat = null;

            if (getDataMat != null)
                getDataMat.Dispose();

            getDataMat = null;
        }

        protected virtual List<string> readClassNames(string filename)
        {
            List<string> classNames = new List<string>();

            System.IO.StreamReader cReader = null;
            try
            {
                cReader = new System.IO.StreamReader(filename, System.Text.Encoding.Default);

                while (cReader.Peek() >= 0)
                {
                    string name = cReader.ReadLine();
                    classNames.Add(name);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError(ex.Message);
                return null;
            }
            finally
            {
                if (cReader != null)
                    cReader.Close();
            }

            return classNames;
        }

        [StructLayout(LayoutKind.Sequential)]
        public readonly struct ClassificationData
        {
            public readonly float cls;
            public readonly float conf;

            // sizeof(ClassificationData)
            public const int Size = 2 * sizeof(float);

            public ClassificationData(int cls, float conf)
            {
                this.cls = cls;
                this.conf = conf;
            }

            public override string ToString()
            {
                return "cls:" + cls.ToString() + " conf:" + conf.ToString();
            }
        };

        public virtual ClassificationData[] getData(Mat results)
        {
            if (results.empty())
                return new ClassificationData[0];

            int num = results.cols();

            if (getDataMat == null)
            {
                getDataMat = new Mat(num, 2, CvType.CV_32FC1);
                float[] arange = Enumerable.Range(0, num).Select(i => (float)i).ToArray();
                getDataMat.col(0).put(0, 0, arange);
            }

            Mat results_numx1 = results.reshape(1, num);
            results_numx1.copyTo(getDataMat.col(1));

            var dst = new ClassificationData[num];
            MatUtils.copyFromMat(getDataMat, dst);

            return dst;
        }

        public virtual ClassificationData[] getSortedData(Mat results, int topK = 5)
        {
            if (results.empty())
                return new ClassificationData[0];

            int num = results.cols();

            if (topK < 1 || topK > num) topK = num;
            var sortedData = getData(results).OrderByDescending(x => x.conf).Take(topK).ToArray();

            return sortedData;
        }

        public virtual ClassificationData getBestMatchData(Mat results)
        {
            if (results.empty())
                return new ClassificationData();

            Core.MinMaxLocResult minmax = Core.minMaxLoc(results);

            return new ClassificationData((int)minmax.maxLoc.x, (float)minmax.maxVal);
        }

        public virtual string getClassLabel(float id)
        {
            int classId = (int)id;
            string className = string.Empty;
            if (classNames != null && classNames.Count != 0)
            {
                if (classId >= 0 && classId < (int)classNames.Count)
                {
                    className = classNames[classId];
                }
            }
            if (string.IsNullOrEmpty(className))
                className = classId.ToString();

            return className;
        }
    }
}