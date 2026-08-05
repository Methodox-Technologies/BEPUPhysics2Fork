using DemoRenderer.UI;
using BEPUPhysics.OpenGLDemos.Helpers;
using BEPUPhysics.OpenGLDemos.Types;
using BEPU.DemoUtilities;
using System;
using System.Numerics;

namespace BEPUPhysics.OpenGLDemos.UI;

struct DemoSwapper
{
    public int TargetDemoIndex;
    bool TrackingInput;

    public void CheckForDemoSwap(DemoHost host)
    {
        if (host.controls.ChangeDemo.WasTriggered(host.loop.Input))
        {
            TrackingInput = !TrackingInput;
            TargetDemoIndex = -1;
        }

        if (TrackingInput)
        {
            for (int i = 0; i < host.loop.Input.TypedCharacters.Count; ++i)
            {
                char character = host.loop.Input.TypedCharacters[i];
                if (character == '\b')
                {
                    //Backspace!
                    if (TargetDemoIndex >= 10)
                        TargetDemoIndex /= 10;
                    else
                        TargetDemoIndex = -1;
                }
                else
                {
                    if (TargetDemoIndex < host.demosSet.Count)
                    {
                        int digit = character - '0';
                        if (digit >= 0 && digit <= 9)
                        {
                            TargetDemoIndex = Math.Max(0, TargetDemoIndex) * 10 + digit;
                        }
                    }
                }
            }

            // Done entering the index. Swap the demo if needed.
            if (host.loop.Input.WasPushed(OpenTK.Windowing.GraphicsLibraryFramework.Keys.Enter))
            {
                TrackingInput = false;
                host.TryChangeToDemo(TargetDemoIndex);
            }
        }

    }

    public void Draw(TextBuilder text, TextBatcher textBatcher, AvailableDemosSet demoSet, Vector2 position, float textHeight, Vector3 textColor, Font font)
    {
        if (TrackingInput)
        {
            text.Clear().Append("Swap demo to: ");
            if (TargetDemoIndex >= 0)
                text.Append(TargetDemoIndex);
            else
                text.Append("_");
            textBatcher.Write(text, position, textHeight, textColor, font);

            float lineSpacing = textHeight * 1.1f;
            position.Y += textHeight * 0.5f;
            textHeight *= 0.8f;
            for (int i = 0; i < demoSet.Count; ++i)
            {
                position.Y += lineSpacing;
                text.Clear().Append(demoSet.GetName(i));
                textBatcher.Write(text.Clear().Append(i).Append(": ").Append(demoSet.GetName(i)), position, textHeight, textColor, font);
            }
        }

    }
}
