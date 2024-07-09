#if !UNITY_WSA_10_0

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.DnnModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.ObjdetectModule;
using OpenCVForUnity.UnityUtils;
using OpenCVForUnity.UnityUtils.Helper;
using OpenCVForUnity.UtilsModule;
using Range = OpenCVForUnity.CoreModule.Range;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// Human Segmentation PPHumanSeg Example
    /// An example of using OpenCV dnn module with Human Segmentation model.
    /// Referring to https://github.com/opencv/opencv_zoo/tree/master/models/human_segmentation_pphumanseg
    /// </summary>
    [RequireComponent(typeof(WebCamTextureToMatHelper))]
    public class OpenCVWebcam : DnnObjectDetectionWebCamTextureExample
    {
        // Human Segmentation
        /// <summary>
        /// The compose bg image toggle.
        /// </summary>
        public Toggle composeBGImageToggle;

        /// <summary>
        /// The hide person toggle.
        /// </summary>
        public Toggle hidePersonToggle;

        /// <summary>
        /// The background image texture.
        /// </summary>
        public Texture2D backGroundImageTexture;

        /// <summary>
        /// The rgb mat.
        /// </summary>
        Mat rgbMat;

        /// <summary>
        /// The mask mat.
        /// </summary>
        Mat maskMat;

        /// <summary>
        /// The background mask mat.
        /// </summary>
        Mat bgMaskMat;

        /// <summary>
        /// The background image mat.
        /// </summary>
        Mat backGroundImageMat;
        

        // Face Detection
        
        [SerializeField] private AppFlowManager _appFlowManager;
        
        [TooltipAttribute("Keep keep_top_k for results outputing.")]
        public int keep_top_k = 750;

        protected Scalar[] pointsColors = new Scalar[] {
            new Scalar(0, 0, 255, 255), // # right eye
            new Scalar(255, 0, 0, 255), // # left eye
            new Scalar(255, 255, 0, 255), // # nose tip
            new Scalar(0, 255, 255, 255), // # mouth right
            new Scalar(0, 255, 0, 255), // # mouth left
            new Scalar(255, 255, 255, 255) };

        PriorBox pb;
        Mat boxes_m_c1;
        Mat boxes_m_c4;
        Mat confidences_m;
        MatOfRect2d boxes;
        MatOfFloat confidences;
        MatOfInt indices;
        

#if UNITY_WEBGL
        IEnumerator getFilePath_Coroutine;
#endif

#if UNITY_WEBGL
        private IEnumerator GetFilePath()
        {
            var getFilePathAsync_0_Coroutine = Utils.getFilePathAsync(MODEL_FILENAME, (result) =>
            {
                hs_model_filepath = result;
            });
            yield return getFilePathAsync_0_Coroutine;

            getFilePath_Coroutine = null;

            Run();
        }
#endif
        
        /// <summary>
        /// Raises the webcam texture to mat helper initialized event.
        /// </summary>
        public override void OnWebCamTextureToMatHelperInitialized()
        {
            Debug.Log("Sub OnWebCamTextureToMatHelperInitialized");
            
            // Face Detecion
            
            base.OnWebCamTextureToMatHelperInitialized();

            Size input_shape = new Size(inpWidth > 0 ? inpWidth : 320, inpHeight > 0 ? inpHeight : 240);
            Size output_shape = bgrMat.size();
            pb = new PriorBox(input_shape, output_shape);
            
            
            // Human Segmentation

            Mat webCamTextureMat = webCamTextureToMatHelper.GetMat();

            texture = new Texture2D(webCamTextureMat.cols(), webCamTextureMat.rows(), TextureFormat.RGBA32, false);
            Utils.matToTexture2D(webCamTextureMat, texture);

            gameObject.GetComponent<Renderer>().material.mainTexture = texture;

            gameObject.transform.localScale = new Vector3(webCamTextureMat.cols(), webCamTextureMat.rows(), 1);
            Debug.Log("Screen.width " + Screen.width + " Screen.height " + Screen.height + " Screen.orientation " + Screen.orientation);

            if (fpsMonitor != null)
            {
                fpsMonitor.Add("width", webCamTextureMat.width().ToString());
                fpsMonitor.Add("height", webCamTextureMat.height().ToString());
                fpsMonitor.Add("orientation", Screen.orientation.ToString());
            }


            float width = webCamTextureMat.width();
            float height = webCamTextureMat.height();

            float widthScale = (float)Screen.width / width;
            float heightScale = (float)Screen.height / height;
            if (widthScale < heightScale)
            {
                Camera.main.orthographicSize = (width * (float)Screen.height / (float)Screen.width) / 2;
            }
            else
            {
                Camera.main.orthographicSize = height / 2;
            }

            rgbMat = new Mat(webCamTextureMat.rows(), webCamTextureMat.cols(), CvType.CV_8UC3);
            maskMat = new Mat(webCamTextureMat.rows(), webCamTextureMat.cols(), CvType.CV_8UC1);

            bgMaskMat = new Mat(webCamTextureMat.rows(), webCamTextureMat.cols(), CvType.CV_8UC1);
            backGroundImageMat = new Mat(webCamTextureMat.size(), CvType.CV_8UC4, new Scalar(39, 255, 86, 255));
            if (backGroundImageTexture != null)
            {
                using (Mat bgMat = new Mat(backGroundImageTexture.height, backGroundImageTexture.width, CvType.CV_8UC4))
                {
                    Utils.texture2DToMat(backGroundImageTexture, bgMat);
                    Imgproc.resize(bgMat, backGroundImageMat, backGroundImageMat.size());
                }
            }
        }

        /// <summary>
        /// Raises the webcam texture to mat helper disposed event.
        /// </summary>
        public void OnWebCamTextureToMatHelperDisposed()
        {
            Debug.Log("OnWebCamTextureToMatHelperDisposed");

            // Human Segmentation
            if (rgbMat != null)
                rgbMat.Dispose();

            if (maskMat != null)
                maskMat.Dispose();

            if (bgMaskMat != null)
                bgMaskMat.Dispose();

            if (backGroundImageMat != null)
                backGroundImageMat.Dispose();

            if (texture != null)
            {
                Texture2D.Destroy(texture);
                texture = null;
            }
            
            // Face Detection
            if (pb != null)
            {
                pb.dispose();
                pb = null;
            }

            if (boxes_m_c1 != null)
                boxes_m_c1.Dispose();
            if (boxes_m_c4 != null)
                boxes_m_c4.Dispose();
            if (confidences_m != null)
                confidences_m.Dispose();
            if (boxes != null)
                boxes.Dispose();
            if (confidences != null)
                confidences.Dispose();
            if (indices != null)
                indices.Dispose();

            boxes_m_c1 = null;
            boxes_m_c4 = null;
            confidences_m = null;
            boxes = null;
            confidences = null;
            indices = null;
        }

        /// <summary>
        /// Raises the webcam texture to mat helper error occurred event.
        /// </summary>
        /// <param name="errorCode">Error code.</param>
        public void OnWebCamTextureToMatHelperErrorOccurred(WebCamTextureToMatHelper.ErrorCode errorCode)
        {
            Debug.Log("OnWebCamTextureToMatHelperErrorOccurred " + errorCode);
        }

        // Update is called once per frame
        protected override void Update()
        {
            if (webCamTextureToMatHelper.IsPlaying() && webCamTextureToMatHelper.DidUpdateThisFrame())
            {
                // Face Detection
                Mat rgbaMat = webCamTextureToMatHelper.GetMat();

                if (net == null)
                {
                    Imgproc.putText(rgbaMat, "model file is not loaded.", new Point(5, rgbaMat.rows() - 30), Imgproc.FONT_HERSHEY_SIMPLEX, 0.7, new Scalar(255, 255, 255, 255), 2, Imgproc.LINE_AA, false);
                    Imgproc.putText(rgbaMat, "Please read console message.", new Point(5, rgbaMat.rows() - 10), Imgproc.FONT_HERSHEY_SIMPLEX, 0.7, new Scalar(255, 255, 255, 255), 2, Imgproc.LINE_AA, false);
                }
                else
                {
                    
                    Imgproc.cvtColor(rgbaMat, bgrMat, Imgproc.COLOR_RGBA2BGR);

                    // Create a 4D blob from a frame.
                    Size inpSize = new Size(inpWidth > 0 ? inpWidth : bgrMat.cols(),
                        inpHeight > 0 ? inpHeight : bgrMat.rows());
                    Mat blob = Dnn.blobFromImage(bgrMat, scale, inpSize, mean, swapRB, false);


                    // Run a model.
                    net.setInput(blob);

                    //TickMeter tm = new TickMeter();
                    //tm.start();

                    List<Mat> outs = new List<Mat>();
                    List<string> output_names = new List<string>();
                    output_names.Add("loc");
                    output_names.Add("conf");
                    output_names.Add("iou");
                    net.forward(outs, output_names);

                    //tm.stop();
                    //Debug.Log("Inference time, ms: " + tm.getTimeMilli());

                    postprocess(rgbaMat, outs, net, Dnn.DNN_BACKEND_OPENCV);

                    for (int i = 0; i < outs.Count; i++)
                    {
                        outs[i].Dispose();
                    }
                    blob.Dispose();
                }

                Utils.matToTexture2D(rgbaMat, texture);
                
                // Human Segmentation
                Mat rgbaMat_2 = webCamTextureToMatHelper.GetMat();

                if (hsNet == null)
                {
                    Imgproc.putText(rgbaMat_2, "model file is not loaded.", new Point(5, rgbaMat_2.rows() - 30), Imgproc.FONT_HERSHEY_SIMPLEX, 0.7, new Scalar(255, 255, 255, 255), 2, Imgproc.LINE_AA, false);
                    Imgproc.putText(rgbaMat_2, "Please read console message.", new Point(5, rgbaMat_2.rows() - 10), Imgproc.FONT_HERSHEY_SIMPLEX, 0.7, new Scalar(255, 255, 255, 255), 2, Imgproc.LINE_AA, false);
                }
                else
                {

                    Imgproc.cvtColor(rgbaMat_2, rgbMat, Imgproc.COLOR_RGBA2RGB);


                    Mat blob = Dnn.blobFromImage(rgbMat, 1.0f / 255.0f, new Size(192, 192), new Scalar(0.5, 0.5, 0.5), false, false, CvType.CV_32F);
                    // Divide blob by std.
                    Core.divide(blob, new Scalar(0.5, 0.5, 0.5), blob);


                    hsNet.setInput(blob);

                    Mat prob = hsNet.forward("save_infer_model/scale_0.tmp_1");

                    Mat result = new Mat();
                    Core.reduceArgMax(prob, result, 1);
                    //result.reshape(0, new int[] { 192,192});
                    result.convertTo(result, CvType.CV_8U, 255.0);
                    
                    Mat mask192x192 = new Mat(192, 192, CvType.CV_8UC1, (IntPtr)result.dataAddr());

                    Imgproc.resize(mask192x192, maskMat, rgbaMat_2.size(), Imgproc.INTER_NEAREST);
                    
                    // Edge blurring
                    /*
                    // Blur the mask to smooth the edges
                    Imgproc.GaussianBlur(maskMat, maskMat, new Size(5, 5), 0);
                    // Apply a threshold to create a binary mask
                    Imgproc.threshold(maskMat, maskMat, 127, 255, Imgproc.THRESH_BINARY);
                    // Define the structuring element for morphological operations
                    Mat kernel = Imgproc.getStructuringElement(Imgproc.MORPH_RECT, new Size(3, 3));
                    // Apply morphological operations to refine the edges
                    Imgproc.morphologyEx(maskMat, maskMat, Imgproc.MORPH_CLOSE, kernel);
                    // Release the kernel
                    kernel.Dispose();
                    */

                    if (composeBGImageToggle.isOn)
                    {
                        // Compose the background image.
                        Core.bitwise_not(maskMat, bgMaskMat);
                        backGroundImageMat.copyTo(rgbaMat_2, bgMaskMat);
                    }

                    if (hidePersonToggle.isOn)
                    {
                        rgbaMat_2.setTo(new Scalar(255, 255, 255, 255), maskMat);
                    }

                    mask192x192.Dispose();
                    result.Dispose();

                    prob.Dispose();
                    blob.Dispose();

                }

                Utils.matToTexture2D(rgbaMat_2, texture);
            }
        }
        
        protected override void postprocess(Mat frame, List<Mat> outs, Net net, int backend = Dnn.DNN_BACKEND_OPENCV)
        {

            // # Decode bboxes and landmarks
            Mat dets = pb.decode(outs[0], outs[1], outs[2]); // "loc", "conf", "iou"


            // # Ignore low scores + NMS
            int num = dets.rows();

            if (boxes_m_c1 == null)
                boxes_m_c1 = new Mat(num, 4, CvType.CV_64FC1);
            if (boxes_m_c4 == null)
                boxes_m_c4 = new Mat(num, 1, CvType.CV_64FC4);
            if (confidences_m == null)
                confidences_m = new Mat(num, 1, CvType.CV_32FC1);

            if (boxes == null)
                boxes = new MatOfRect2d(boxes_m_c4);
            if (confidences == null)
                confidences = new MatOfFloat(confidences_m);
            if (indices == null)
                indices = new MatOfInt();

            Mat bboxes = dets.colRange(0, 4);
            bboxes.convertTo(boxes_m_c1, CvType.CV_64FC1);
            MatUtils.copyToMat(new IntPtr(boxes_m_c1.dataAddr()), boxes_m_c4);

            Mat scores = dets.colRange(14, 15);
            scores.copyTo(confidences_m);

            Dnn.NMSBoxes(boxes, confidences, confThreshold, nmsThreshold, indices, 1f, keep_top_k);

            if (indices.total() >= 1) _appFlowManager.FaceDetected = true;
            else _appFlowManager.FaceDetected = false;

            // # Draw boudning boxes and landmarks on the original image
            for (int i = 0; i < indices.total(); ++i)
            {
                int idx = (int)indices.get(i, 0)[0];

                float[] bbox_arr = new float[4];
                bboxes.get(idx, 0, bbox_arr);
                float[] confidence_arr = new float[1];
                confidences.get(idx, 0, confidence_arr);
                drawPred(0, confidence_arr[0], bbox_arr[0], bbox_arr[1], bbox_arr[0] + bbox_arr[2], bbox_arr[1] + bbox_arr[3], frame);

                Mat landmarks = dets.colRange(4, 14);
                float[] landmarks_arr = new float[10];
                landmarks.get(idx, 0, landmarks_arr);
                Point[] points = new Point[] { new Point(landmarks_arr[0], landmarks_arr[1]), new Point(landmarks_arr[2], landmarks_arr[3]),
                    new Point(landmarks_arr[4], landmarks_arr[5]), new Point(landmarks_arr[6], landmarks_arr[7]), new Point(landmarks_arr[8], landmarks_arr[9])};
                drawPredPoints(points, frame);
            }
            
        }
        
        protected virtual void drawPredPoints(Point[] points, Mat frame)
        {
            for (int i = 0; i < points.Length; i++)
            {
                if (i < pointsColors.Length)
                {
                    Imgproc.circle(frame, points[i], 2, pointsColors[i], 2);
                }
                else
                {
                    Imgproc.circle(frame, points[i], 2, pointsColors[pointsColors.Length - 1], 2);
                }
            }
        }

        private class PriorBox
        {
            float[][] min_sizes = new float[][]{
                new float[]{10.0f,  16.0f,  24.0f},
                new float[]{32.0f,  48.0f},
                new float[]{64.0f,  96.0f},
                new float[]{128.0f, 192.0f, 256.0f}
            };

            int[] steps = new int[] { 8, 16, 32, 64 };
            float[] variance = new float[] { 0.1f, 0.2f };

            int in_w;
            int in_h;
            int out_w;
            int out_h;

            List<Size> feature_map_sizes;
            Mat priors;

            Mat dets;
            Mat ones;
            Mat scale;

            Mat priors_0_2;
            Mat priors_2_4;
            Mat bboxes;
            Mat bboxes_0_2;
            Mat bboxes_2_4;
            Mat landmarks;
            Mat landmarks_0_2;
            Mat landmarks_2_4;
            Mat landmarks_4_6;
            Mat landmarks_6_8;
            Mat landmarks_8_10;
            Mat scores;
            Mat ones_0_1;
            Mat ones_0_2;
            Mat bbox_scale;
            Mat landmark_scale;

            public PriorBox(Size input_shape, Size output_shape)
            {
                // initialize
                in_w = (int)input_shape.width;
                in_h = (int)input_shape.height;
                out_w = (int)output_shape.width;
                out_h = (int)output_shape.height;

                Size feature_map_2nd = new Size((int)((int)((in_w + 1) / 2) / 2), (int)((int)((in_h + 1) / 2) / 2));
                Size feature_map_3rd = new Size((int)(feature_map_2nd.width / 2), (int)(feature_map_2nd.height / 2));
                Size feature_map_4th = new Size((int)(feature_map_3rd.width / 2), (int)(feature_map_3rd.height / 2));
                Size feature_map_5th = new Size((int)(feature_map_4th.width / 2), (int)(feature_map_4th.height / 2));
                Size feature_map_6th = new Size((int)(feature_map_5th.width / 2), (int)(feature_map_5th.height / 2));

                feature_map_sizes = new List<Size>();
                feature_map_sizes.Add(feature_map_3rd);
                feature_map_sizes.Add(feature_map_4th);
                feature_map_sizes.Add(feature_map_5th);
                feature_map_sizes.Add(feature_map_6th);

                priors = generate_prior();
                priors_0_2 = priors.colRange(new Range(0, 2));
                priors_2_4 = priors.colRange(new Range(2, 4));
            }

            private Mat generate_prior()
            {
                int priors_size = 0;
                for (int index = 0; index < feature_map_sizes.Count; index++)
                    priors_size += (int)(feature_map_sizes[index].width * feature_map_sizes[index].height * min_sizes[index].Length);

                Mat anchors = new Mat(priors_size, 4, CvType.CV_32FC1);
                int count = 0;
                for (int i = 0; i < feature_map_sizes.Count; i++)
                {
                    Size feature_map_size = feature_map_sizes[i];
                    float[] min_size = min_sizes[i];

                    for (int _h = 0; _h < feature_map_size.height; _h++)
                    {
                        for (int _w = 0; _w < feature_map_size.width; _w++)
                        {
                            for (int j = 0; j < min_size.Length; j++)
                            {
                                float s_kx = min_size[j] / in_w;
                                float s_ky = min_size[j] / in_h;

                                float cx = (float)((_w + 0.5) * steps[i] / in_w);
                                float cy = (float)((_h + 0.5) * steps[i] / in_h);

                                anchors.put(count, 0, new float[] { cx, cy, s_kx, s_ky });

                                count++;
                            }
                        }
                    }
                }

                return anchors;
            }

            /// <summary>
            /// Decodes the locations (x1, y1, w, h,...) and scores (c) from the priors, and the given loc and conf.
            /// </summary>
            /// <param name="loc">loc produced from loc layers of shape [num_priors, 14]. '14' for [x_c, y_c, w, h,...].</param>
            /// <param name="conf">conf produced from conf layers of shape [num_priors, 2]. '2' for [p_non_face, p_face].</param>
            /// <param name="iou">iou produced from iou layers of shape [num_priors, 1]. '1' for [iou].</param>
            /// <returns>dets is concatenated by bboxes, landmarks and scores. num * [x1, y1, w, h, x_re, y_re, x_le, y_le, x_n, y_n, x_mr, y_mr, x_ml, y_ml, score]</returns>
            public Mat decode(Mat loc, Mat conf, Mat iou)
            {
                Mat loc_m = loc; // [num*14]
                Mat conf_m = conf; // [num*2]
                Mat iou_m = iou; // [num*1]

                int num = loc_m.rows();

                if (dets == null || (dets != null && dets.IsDisposed))
                {
                    dets = new Mat(num, 15, CvType.CV_32FC1);
                    ones = Mat.ones(num, 2, CvType.CV_32FC1);
                    scale = new Mat(num, 1, CvType.CV_32FC4, new Scalar(out_w, out_h, out_w, out_h));
                    scale = scale.reshape(1, num);

                    bboxes = dets.colRange(new Range(0, 4));
                    bboxes_0_2 = bboxes.colRange(new Range(0, 2));
                    bboxes_2_4 = bboxes.colRange(new Range(2, 4));
                    landmarks = dets.colRange(new Range(4, 14));
                    landmarks_0_2 = landmarks.colRange(new Range(0, 2));
                    landmarks_2_4 = landmarks.colRange(new Range(2, 4));
                    landmarks_4_6 = landmarks.colRange(new Range(4, 6));
                    landmarks_6_8 = landmarks.colRange(new Range(6, 8));
                    landmarks_8_10 = landmarks.colRange(new Range(8, 10));
                    scores = dets.colRange(new Range(14, 15));
                    ones_0_1 = ones.colRange(0, 1);
                    ones_0_2 = ones.colRange(0, 2);
                    bbox_scale = scale.colRange(0, 4);
                    landmark_scale = scale.colRange(0, 2);
                    
                }


                Mat loc_0_2 = loc_m.colRange(new Range(0, 2));
                Mat loc_2_4 = loc_m.colRange(new Range(2, 4));
                Mat loc_2_3 = loc_m.colRange(new Range(2, 3));
                Mat loc_3_4 = loc_m.colRange(new Range(3, 4));

                // # get bboxes
                Core.multiply(loc_0_2, priors_2_4, bboxes_0_2, variance[0]);
                Core.add(priors_0_2, bboxes_0_2, bboxes_0_2);
                Core.multiply(loc_2_3, ones_0_1, loc_2_3, variance[0]);
                Core.multiply(loc_3_4, ones_0_1, loc_3_4, variance[1]);
                Core.exp(loc_2_4, bboxes_2_4);
                Core.multiply(priors_2_4, bboxes_2_4, bboxes_2_4);

                // # (x_c, y_c, w, h) -> (x1, y1, w, h)
                Core.divide(bboxes_2_4, ones_0_2, loc_2_4, 0.5);
                Core.subtract(bboxes_0_2, loc_2_4, bboxes_0_2);

                // # scale recover
                Core.multiply(bboxes, bbox_scale, bboxes);


                Mat loc_4_6 = loc_m.colRange(new Range(4, 6));
                Mat loc_6_8 = loc_m.colRange(new Range(6, 8));
                Mat loc_8_10 = loc_m.colRange(new Range(8, 10));
                Mat loc_10_12 = loc_m.colRange(new Range(10, 12));
                Mat loc_12_14 = loc_m.colRange(new Range(12, 14));

                // # get landmarks
                Core.multiply(loc_4_6, priors_2_4, landmarks_0_2, variance[0]);
                Core.add(priors_0_2, landmarks_0_2, landmarks_0_2);
                Core.multiply(loc_6_8, priors_2_4, landmarks_2_4, variance[0]);
                Core.add(priors_0_2, landmarks_2_4, landmarks_2_4);
                Core.multiply(loc_8_10, priors_2_4, landmarks_4_6, variance[0]);
                Core.add(priors_0_2, landmarks_4_6, landmarks_4_6);
                Core.multiply(loc_10_12, priors_2_4, landmarks_6_8, variance[0]);
                Core.add(priors_0_2, landmarks_6_8, landmarks_6_8);
                Core.multiply(loc_12_14, priors_2_4, landmarks_8_10, variance[0]);
                Core.add(priors_0_2, landmarks_8_10, landmarks_8_10);

                // # scale recover
                Core.multiply(landmarks_0_2, landmark_scale, landmarks_0_2);
                Core.multiply(landmarks_2_4, landmark_scale, landmarks_2_4);
                Core.multiply(landmarks_4_6, landmark_scale, landmarks_4_6);
                Core.multiply(landmarks_6_8, landmark_scale, landmarks_6_8);
                Core.multiply(landmarks_8_10, landmark_scale, landmarks_8_10);


                // # get score
                Mat cls_scores = conf_m.colRange(new Range(1, 2));
                Mat iou_scores = iou_m;
                Imgproc.threshold(iou_scores, iou_scores, 0, 0, Imgproc.THRESH_TOZERO);
                Imgproc.threshold(iou_scores, iou_scores, 1.0, 0, Imgproc.THRESH_TRUNC);
                Core.multiply(cls_scores, iou_scores, scores);
                Core.sqrt(scores, scores);

                return dets;
            }

            public void dispose()
            {
                if (priors != null)
                    priors.Dispose();

                if (dets != null)
                    dets.Dispose();
                if (ones != null)
                    ones.Dispose();
                if (scale != null)
                    scale.Dispose();
            }
        }

        /// <summary>
        /// Raises the destroy event.
        /// </summary>
        void OnDestroy()
        {
            webCamTextureToMatHelper.Dispose();

            if (hsNet != null)
                hsNet.Dispose();

            Utils.setDebugMode(false);

#if UNITY_WEBGL
            if (getFilePath_Coroutine != null)
            {
                StopCoroutine(getFilePath_Coroutine);
                ((IDisposable)getFilePath_Coroutine).Dispose();
            }
#endif
        }

        /// <summary>
        /// Raises the back button click event.
        /// </summary>
        public void OnBackButtonClick()
        {
            SceneManager.LoadScene("OpenCVForUnityExample");
        }

        /// <summary>
        /// Raises the play button click event.
        /// </summary>
        public void OnPlayButtonClick()
        {
            webCamTextureToMatHelper.Play();
        }

        /// <summary>
        /// Raises the pause button click event.
        /// </summary>
        public void OnPauseButtonClick()
        {
            webCamTextureToMatHelper.Pause();
        }

        /// <summary>
        /// Raises the stop button click event.
        /// </summary>
        public void OnStopButtonClick()
        {
            webCamTextureToMatHelper.Stop();
        }

        /// <summary>
        /// Raises the change camera button click event.
        /// </summary>
        public void OnChangeCameraButtonClick()
        {
            webCamTextureToMatHelper.requestedIsFrontFacing = !webCamTextureToMatHelper.requestedIsFrontFacing;
        }
    }
}

#endif