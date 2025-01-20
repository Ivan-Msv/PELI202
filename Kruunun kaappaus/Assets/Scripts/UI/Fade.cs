using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class Fade : MonoBehaviour
{
    private float fadeSpeed;
    private List<GameObject> currentlyFading = new();

    public void StartFade(Transform fadeObject, bool fadeIn, float speed = 1)
    {
        fadeSpeed = speed;
        var component = GetProperComponent(fadeObject);
        StartCoroutine(FadeCoroutine(component, fadeIn));
    }

    private IEnumerator FadeCoroutine(Component givenComponent, bool fadeIn)
    {
        if (givenComponent == null)
        {
            yield break;
        }

        var componentObject = givenComponent.gameObject;

        while (currentlyFading.Contains(componentObject))
        {
            yield return null;
        }

        currentlyFading.Add(componentObject);
        Color newColor;

        if (givenComponent is SpriteRenderer sprite)
        {
            newColor = sprite.color;
            switch (fadeIn)
            {
                case true:
                    while (sprite.color.a < 1 && currentlyFading.Contains(componentObject))
                    {
                        newColor.a += fadeSpeed * Time.deltaTime;
                        sprite.color = newColor;
                        yield return null;
                    }
                    break;
                case false:
                    while (sprite.color.a > 0 && currentlyFading.Contains(componentObject))
                    {
                        newColor.a -= fadeSpeed * Time.deltaTime;
                        sprite.color = newColor;
                        yield return null;
                    }
                    break;
            }
        }

        else if (givenComponent is Image image)
        {
            newColor = image.color;
            switch (fadeIn)
            {
                case true:
                    while (image.color.a < 1 && currentlyFading.Contains(componentObject))
                    {
                        newColor.a += fadeSpeed * Time.deltaTime;
                        image.color = newColor;
                        yield return null;
                    }
                    break;
                case false:
                    while (image.color.a > 0 && currentlyFading.Contains(componentObject))
                    {
                        newColor.a -= fadeSpeed * Time.deltaTime;
                        image.color = newColor;
                        yield return null;
                    }
                    break;
            }
        }

        else if (givenComponent is CanvasGroup canvas)
        {
            switch (fadeIn)
            {
                case true:
                    while (canvas.alpha < 1 && currentlyFading.Contains(componentObject))
                    {
                        canvas.alpha += fadeSpeed * Time.deltaTime;
                        yield return null;
                    }
                    break;
                case false:
                    while (canvas.alpha > 0 && currentlyFading.Contains(componentObject))
                    {
                        canvas.alpha -= fadeSpeed * Time.deltaTime;
                        yield return null;
                    }
                    break;
            }
        }

        currentlyFading.Remove(componentObject);
    }
    private Component GetProperComponent(Transform fadeObject)
    {
        fadeObject.TryGetComponent<SpriteRenderer>(out var sprite);

        if (sprite != null)
        {
            return sprite;
        }

        fadeObject.TryGetComponent<Image>(out var image);

        if (image != null)
        {
            return image;
        }

        fadeObject.TryGetComponent<CanvasGroup>(out var canvasGroup);

        if (canvasGroup != null)
        {
            return canvasGroup;
        }

        Debug.LogError($"Could not find appropriate component for {fadeObject}, are you using this right?");
        return null;
    }
}
