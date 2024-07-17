using System;
using OpenCVForUnityExample;
using UnityEngine;
using UnityEngine.UI;

public class ImageCropper : MonoBehaviour
{
    public StandaloneHumanSegmentation Shs;
    //public Texture2D originalImage;
    public Image leftImage;
    public Image rightImage;

    private Texture2D originalImage;

    private void Awake()
    {
        string fileName = ConfigHandler.BackgroundFileName; // Replace with your image filename
        string filePath = Application.streamingAssetsPath + "/" + fileName;

        byte[] data = System.IO.File.ReadAllBytes(filePath);
        originalImage = new Texture2D(2, 2);
        originalImage.LoadImage(data);

        Shs.backGroundImageTexture = originalImage;
        
        CropImage();
    }

    public void CropImage()
    {
        // Define crop rectangles (adjust these values based on your needs)
        Rect leftCropRect = new Rect(0, 0, 512, 1080);
        Rect rightCropRect = new Rect(originalImage.width - 512, 0, 512, 1080);

        // Get pixel data
        Color[] originalPixels = originalImage.GetPixels();

        // Extract pixels for left and right images
        Color[] leftPixels = GetCropPixels(originalPixels, leftCropRect);
        Color[] rightPixels = GetCropPixels(originalPixels, rightCropRect);

        // Create new textures
        Texture2D leftTexture = new Texture2D(512, 1080);
        Texture2D rightTexture = new Texture2D(512, 1080);

        // Set pixel data
        leftTexture.SetPixels(leftPixels); 
        rightTexture.SetPixels(rightPixels);

        // Apply changes
        leftTexture.Apply();
        rightTexture.Apply();

        // Create sprites
        Sprite leftSprite = Sprite.Create(leftTexture, leftCropRect, Vector2.zero);
        Sprite rightSprite = Sprite.Create(rightTexture, new Rect(0, 0, 512, 1080), Vector2.zero);

        // Assign sprites to image components
        leftImage.sprite = leftSprite;
        rightImage.sprite = rightSprite;
    }

    private Color[] GetCropPixels(Color[] originalPixels, Rect cropRect)
    {
        int width = originalImage.width;
        int height = originalImage.height;

        int cropX = Mathf.FloorToInt(cropRect.x);
        int cropY = Mathf.FloorToInt(cropRect.y);
        int cropWidth = Mathf.FloorToInt(cropRect.width);
        int cropHeight = Mathf.FloorToInt(cropRect.height);

        Color[] croppedPixels = new Color[cropWidth * cropHeight];
        int index = 0;

        for (int y = cropY; y < cropY + cropHeight; y++)
        {
            for (int x = cropX; x < cropX + cropWidth; x++)
            {
                croppedPixels[index] = originalPixels[y * width + x];
                index++;
            }
        }

        return croppedPixels;
    }
}
