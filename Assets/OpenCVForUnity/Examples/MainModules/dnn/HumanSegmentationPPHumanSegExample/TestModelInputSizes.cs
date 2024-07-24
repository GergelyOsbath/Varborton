using System;
using UnityEngine;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.DnnModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityUtils;

public class TestModelInputSizes : MonoBehaviour
{
    Net net;
    Mat rgbMat;
    
    protected static readonly string MODEL_FILENAME = "OpenCVForUnity/dnn/human_segmentation_pphumanseg_2023mar.onnx";
    /// <summary>
    /// The model filepath.
    /// </summary>
    string model_filepath;
    void Start()
    {
        model_filepath = Utils.getFilePath(MODEL_FILENAME);
        
        try
        {
            net = Dnn.readNet(model_filepath);
            
            if (net.empty())
            {
                Debug.LogError("Failed to load model from " + model_filepath);
                return;
            }
            
            Debug.Log("Model loaded successfully from " + model_filepath);
            
            // Example input sizes to test
            int[] inputSizes = { 192, 256, 384, 512 };
            
            foreach (int size in inputSizes)
            {
                TestInputSize(size);
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Exception caught while loading model: " + e.Message);
        }
    }

    void TestInputSize(int size)
    {
        try
        {
            // Create a dummy input image with the specified size
            Mat dummyInput = new Mat(size, size, CvType.CV_8UC3, new Scalar(0, 0, 0));
            rgbMat = new Mat();
            Imgproc.cvtColor(dummyInput, rgbMat, Imgproc.COLOR_RGBA2RGB);
            
            // Create a blob from the input image
            Mat blob = Dnn.blobFromImage(rgbMat, 1.0 / 255.0, new Size(size, size), new Scalar(0.5, 0.5, 0.5), false, false, CvType.CV_32F);
            Core.divide(blob, new Scalar(0.5, 0.5, 0.5), blob);
            
            // Set the input to the network
            net.setInput(blob);
            
            // Forward pass to get the output
            Mat prob = net.forward();
            
            // Check the output size and any potential errors
            Debug.Log($"Input Size: {size}x{size}, Output Size: {prob.size()}");
            
            // Clean up
            prob.Dispose();
            blob.Dispose();
        }
        catch (Exception e)
        {
            Debug.LogError($"Exception caught while processing input size {size}: " + e.Message + "\n" + e.StackTrace);
        }
    }
}
