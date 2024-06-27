#if !UNITY_WSA_10_0

using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgcodecsModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityUtils;
using OpenCVForUnity.UnityUtils.Helper;
using OpenCVForUnityExample.DnnModel;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// Pose Estimation MediaPipe Example
    /// An example of using OpenCV dnn module with Human Pose Estimation.
    /// Referring to https://github.com/opencv/opencv_zoo/tree/main/models/pose_estimation_mediapipe
    /// </summary>
    [RequireComponent(typeof(WebCamTextureToMatHelper))]
    public class PoseEstimationMediaPipe_PHS : MonoBehaviour
    {
        /// <summary>
        /// The texture.
        /// </summary>
        Texture2D texture;

        YOLOXObjectDetector objectDetector;
        
        /// <summary>
        /// The webcam texture to mat helper.
        /// </summary>
        WebCamTextureToMatHelper webCamTextureToMatHelper;

        /// <summary>
        /// The bgr mat.
        /// </summary>
        Mat bgrMat;

        /// <summary>
        /// The person detector.
        /// </summary>
        MediaPipePersonDetector personDetector;

        /// <summary>
        /// The pose estimator.
        /// </summary>
        MediaPipePoseEstimator poseEstimator;

        /// <summary>
        /// The FPS monitor.
        /// </summary>
        FpsMonitor fpsMonitor;

        /// <summary>
        /// PERSON_DETECTION_MODEL_FILENAME
        /// </summary>
        protected static readonly string PERSON_DETECTION_MODEL_FILENAME = "OpenCVForUnity/dnn/person_detection_mediapipe_2023mar.onnx";

        /// <summary>
        /// The person detection model filepath.
        /// </summary>
        string person_detection_model_filepath;

        /// <summary>
        /// POSE_ESTIMATION_MODEL_FILENAME
        /// </summary>
        protected static readonly string POSE_ESTIMATION_MODEL_FILENAME = "OpenCVForUnity/dnn/pose_estimation_mediapipe_2023mar.onnx";

        /// <summary>
        /// The pose estimation model filepath.
        /// </summary>
        string pose_estimation_model_filepath;

        [TooltipAttribute("Path to a binary file of model contains trained weights. It could be a file with extensions .caffemodel (Caffe), .pb (TensorFlow), .t7 or .net (Torch), .weights (Darknet).")]
        public string model = "yolox_tiny.onnx";

        [TooltipAttribute("Path to a text file of model contains network configuration. It could be a file with extensions .prototxt (Caffe), .pbtxt (TensorFlow), .cfg (Darknet).")]
        public string config = "";

        [TooltipAttribute("Optional path to a text file with names of classes to label detected objects.")]
        public string classes = "coco.names";
        
        
        [TooltipAttribute("Confidence threshold.")]
        public float confThreshold = 0.25f;
        [TooltipAttribute("Confidence threshold.")]
        public float poseEstConfThreshold = 0.9f;

        [TooltipAttribute("Non-maximum suppression threshold.")]
        public float nmsThreshold = 0.45f;

        [TooltipAttribute("Maximum detections per image.")]
        public int topK = 1000;

        [TooltipAttribute("Preprocess input image by resizing to a specific width.")]
        public int inpWidth = 416;

        [TooltipAttribute("Preprocess input image by resizing to a specific height.")]
        public int inpHeight = 416;
        
        protected string classes_filepath;
        protected string config_filepath;
        protected string model_filepath;


#if UNITY_WEBGL
        IEnumerator getFilePath_Coroutine;
#endif

        // Use this for initialization
        void Start()
        {
            fpsMonitor = GetComponent<FpsMonitor>();

            webCamTextureToMatHelper = gameObject.GetComponent<WebCamTextureToMatHelper>();

            
            if (!string.IsNullOrEmpty(classes))
            {
                classes_filepath = Utils.getFilePath("OpenCVForUnity/dnn/" + classes);
                if (string.IsNullOrEmpty(classes_filepath)) Debug.Log("The file:" + classes + " did not exist in the folder “Assets/StreamingAssets/OpenCVForUnity/dnn”.");
            }
            if (!string.IsNullOrEmpty(config))
            {
                config_filepath = Utils.getFilePath("OpenCVForUnity/dnn/" + config);
                if (string.IsNullOrEmpty(config_filepath)) Debug.Log("The file:" + config + " did not exist in the folder “Assets/StreamingAssets/OpenCVForUnity/dnn”.");
            }
            if (!string.IsNullOrEmpty(model))
            {
                model_filepath = Utils.getFilePath("OpenCVForUnity/dnn/" + model);
                if (string.IsNullOrEmpty(model_filepath)) Debug.Log("The file:" + model + " did not exist in the folder “Assets/StreamingAssets/OpenCVForUnity/dnn”.");
            }
            
#if UNITY_WEBGL
            getFilePath_Coroutine = GetFilePath();
            StartCoroutine(getFilePath_Coroutine);
#else
            person_detection_model_filepath = Utils.getFilePath(PERSON_DETECTION_MODEL_FILENAME);
            pose_estimation_model_filepath = Utils.getFilePath(POSE_ESTIMATION_MODEL_FILENAME);
            Run();
#endif
        }

        
        
#if UNITY_WEBGL
        private IEnumerator GetFilePath()
        {
            var getFilePathAsync_0_Coroutine = Utils.getFilePathAsync(PERSON_DETECTION_MODEL_FILENAME, (result) =>
            {
                person_detection_model_filepath = result;
            });
            yield return getFilePathAsync_0_Coroutine;

            var getFilePathAsync_1_Coroutine = Utils.getFilePathAsync(POSE_ESTIMATION_MODEL_FILENAME, (result) =>
            {
                pose_estimation_model_filepath = result;
            });
            yield return getFilePathAsync_1_Coroutine;

            getFilePath_Coroutine = null;

            Run();
        }
#endif

        // Use this for initialization
        void Run()
        {
            //if true, The error log of the Native side OpenCV will be displayed on the Unity Editor Console.
            Utils.setDebugMode(true);


            if (string.IsNullOrEmpty(person_detection_model_filepath))
            {
                Debug.LogError(PERSON_DETECTION_MODEL_FILENAME + " is not loaded. Please read “StreamingAssets/OpenCVForUnity/dnn/setup_dnn_module.pdf” to make the necessary setup.");
            }
            else
            {
                personDetector = new MediaPipePersonDetector(person_detection_model_filepath, nmsThreshold, confThreshold, topK);// # usually only one person has good performance
            }

            if (string.IsNullOrEmpty(pose_estimation_model_filepath))
            {
                Debug.LogError(POSE_ESTIMATION_MODEL_FILENAME + " is not loaded. Please read “StreamingAssets/OpenCVForUnity/dnn/setup_dnn_module.pdf” to make the necessary setup.");
            }
            else
            {
                poseEstimator = new MediaPipePoseEstimator(pose_estimation_model_filepath, poseEstConfThreshold, 3, 2);
            }

            if (string.IsNullOrEmpty(model_filepath) || string.IsNullOrEmpty(classes_filepath))
            {
                Debug.LogError("model: " + model + " or " + "config: " + config + " or " + "classes: " + classes + " is not loaded. Please read “StreamingAssets/OpenCVForUnity/dnn/setup_dnn_module.pdf” to make the necessary setup.");
            }
            else
            {
                objectDetector = new YOLOXObjectDetector(model_filepath, config_filepath, classes_filepath, new Size(inpWidth, inpHeight), confThreshold, nmsThreshold, topK);
            }

#if UNITY_ANDROID && !UNITY_EDITOR
            // Avoids the front camera low light issue that occurs in only some Android devices (e.g. Google Pixel, Pixel2).
            webCamTextureToMatHelper.avoidAndroidFrontCameraLowLightIssue = true;
#endif
            webCamTextureToMatHelper.Initialize();
            
        }

        /// <summary>
        /// Raises the webcam texture to mat helper initialized event.
        /// </summary>
        public void OnWebCamTextureToMatHelperInitialized()
        {
            Debug.Log("OnWebCamTextureToMatHelperInitialized");

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

            bgrMat = new Mat(webCamTextureMat.rows(), webCamTextureMat.cols(), CvType.CV_8UC3);
        }

        /// <summary>
        /// Raises the webcam texture to mat helper disposed event.
        /// </summary>
        public void OnWebCamTextureToMatHelperDisposed()
        {
            Debug.Log("OnWebCamTextureToMatHelperDisposed");

            if (bgrMat != null)
                bgrMat.Dispose();

            if (texture != null)
            {
                Texture2D.Destroy(texture);
                texture = null;
            }
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
        void Update()
        {

            if (webCamTextureToMatHelper.IsPlaying() && webCamTextureToMatHelper.DidUpdateThisFrame())
            {

                Mat rgbMat = webCamTextureToMatHelper.GetMat();

                Debug.Log($"res: {rgbMat.rows()}/{rgbMat.cols()}");
                
                if (personDetector == null || poseEstimator == null)
                {
                    Imgproc.putText(rgbMat, "model file is not loaded.", new Point(5, rgbMat.rows() - 30), Imgproc.FONT_HERSHEY_SIMPLEX, 0.7, new Scalar(255, 255, 255, 255), 2, Imgproc.LINE_AA, false);
                    Imgproc.putText(rgbMat, "Please read console message.", new Point(5, rgbMat.rows() - 10), Imgproc.FONT_HERSHEY_SIMPLEX, 0.7, new Scalar(255, 255, 255, 255), 2, Imgproc.LINE_AA, false);
                }
                else
                {
                    Imgproc.cvtColor(rgbMat, bgrMat, Imgproc.COLOR_RGB2BGR);

                    //TickMeter tm = new TickMeter();
                    //tm.start();

                    
                    Mat persons = personDetector.infer(bgrMat);
                    
                    if (persons.rows() > 0) Debug.Log($"Persons Found {persons.rows()}");
                    
                    
                    // Estimate the pose of each person
                    for (int i = 0; i < persons.rows(); ++i)
                    {
                        //tm.reset();
                        //tm.start();

                        // pose estimator inference
                        List<Mat> results = poseEstimator.infer(bgrMat, persons.row(i), false);
                        //tm.stop();
                        //Debug.Log("MediaPipePoseEstimator Inference time (preprocess + infer + postprocess), ms: " + tm.getTimeMilli());
                        
                        poseEstimator.visualize(bgrMat, results[0], false, false);
                        
                        //Debug.Log($"Pose Found {i}");
                        
                    }

                    //personDetector.visualize(bgrMat, persons, false, false);
                }

                Imgproc.cvtColor(bgrMat, rgbMat, Imgproc.COLOR_BGR2RGBA);

                Utils.matToTexture2D(rgbMat, texture);
            }

        }


        /// <summary>
        /// Raises the destroy event.
        /// </summary>
        void OnDestroy()
        {
            webCamTextureToMatHelper.Dispose();

            if (personDetector != null)
                personDetector.dispose();

            if (poseEstimator != null)
                poseEstimator.dispose();

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