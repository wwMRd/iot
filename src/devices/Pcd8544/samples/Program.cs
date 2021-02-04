﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Device.Spi;
using System.Device.Gpio;
using System.Device.Pwm.Drivers;
using System.IO;
using System.Device.Pwm;
using System;
using System.Threading;
using Iot.Device.Display;
using Iot.Device.Ft4222;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

Console.WriteLine("Hello PCD8544, screen of Nokia 5110!");
Console.WriteLine("Please select the platform you want to use:");
Console.WriteLine("  1. Native device like a Raspberry Pi");
Console.WriteLine("  2. FT4222");
var platformChoice = Console.ReadKey();
Console.WriteLine();

if (platformChoice is not object or { KeyChar: not ('1' or '2') })
{
    Console.WriteLine("You have to choose a valid platform");
    return;
}

SpiConnectionSettings spiConnection = new(0, 1) { ClockFrequency = 5_000_000, Mode = SpiMode.Mode0, DataFlow = DataFlow.MsbFirst, ChipSelectLineActiveState = PinValue.Low };
Pcd8544 lcd;

int pwmPin = -1;
Console.WriteLine("Which pin/channel do you want to use for PWM? (use negative number for none)");
var pinChoice = Console.ReadLine();
try
{
    pwmPin = Convert.ToInt32(pinChoice);
}
catch
{
    Console.WriteLine("Can't convert the pin number, will assume you don't want to use PWM.");
}

int resetPin = -1;
Console.WriteLine("Which pin do you want to use for Reset? (use negative number for none)");
pinChoice = Console.ReadLine();
try
{
    resetPin = Convert.ToInt32(pinChoice);
}
catch
{
    Console.WriteLine("Can't convert the pin number, will assume you don't want to use Reset.");
}

int dataCommandPin = -1;
Console.WriteLine("Which pin do you want to use for Data Command?");
pinChoice = Console.ReadLine();
try
{
    dataCommandPin = Convert.ToInt32(pinChoice);
}
catch
{
}

if (dataCommandPin < 0)
{
    Console.WriteLine("Can't continue as Data Command pin has to be valid.");
    return;
}

GpioController? gpio = null;
if (platformChoice.KeyChar == '1')
{
    PwmChannel? pwmChannel = pwmPin >= 0 ? PwmChannel.Create(0, pwmPin) : null;
    SpiDevice spi = SpiDevice.Create(spiConnection);
    lcd = new(dataCommandPin, spi, resetPin, pwmChannel);
}
else
{
    gpio = new(PinNumberingScheme.Logical, new Ft4222Gpio());
    Ft4222Spi ft4222Spi = new(spiConnection);
    SoftwarePwmChannel? softPwm = pwmPin >= 0 ? new(pwmPin, 1000, usePrecisionTimer: true, controller: gpio, shouldDispose: false, dutyCycle: 0) : null;
    lcd = new(dataCommandPin, ft4222Spi, resetPin, softPwm, gpio, false);
}

if (lcd is not object)
{
    Console.WriteLine("Something went wrong, can't initialize PCD8544");
    return;
}

Console.WriteLine("Choose the demonstration you want to see:");
Console.WriteLine("  1. All");
Console.WriteLine("  2. Brightness, Contrast, Temperature, Bias");
Console.WriteLine("  3. Display text, change cursor position");
Console.WriteLine("  4. Display images, resize images");
Console.WriteLine("  5. Display lines, points, rectangles");
var demoChoice = Console.ReadKey();
Console.WriteLine();

if (demoChoice is not object or { KeyChar: not ('1' or '2' or '3' or '4' or '5') })
{
    Console.WriteLine("You have to choose a demonstration");
    return;
}

switch (demoChoice.KeyChar)
{
    case '1':
        BrightnessContrastTemperatureBias();
        DisplayTextChangePositionBlink();
        DisplayingBitmap();
        DisplayLinesPointsRectabngles();
        break;
    case '2':
        BrightnessContrastTemperatureBias();
        break;
    case '3':
        DisplayTextChangePositionBlink();
        break;
    case '4':
        DisplayingBitmap();
        break;
    case '5':
        DisplayLinesPointsRectabngles();
        break;
}

void BrightnessContrastTemperatureBias()
{
    Console.WriteLine("Adjusting brightness from 0 to 1.0");
    for (int i = 0; i < 10; i++)
    {
        lcd.SetCursorPosition(0, 0);
        lcd.WriteLine("Test for brightness");
        lcd.Write($"{i * 10} %");
        lcd.BacklightBrightness = i / 10.0f;
        Thread.Sleep(1000);
    }

    lcd.Clear();
    Console.WriteLine("Reseting brightness to 0.2 (20%)");
    lcd.BacklightBrightness = 0.2f;

    Console.WriteLine("Adjusting contrast from 0 to 127");
    for (byte i = 0; i < 128; i++)
    {
        lcd.SetCursorPosition(0, 0);
        lcd.WriteLine("Test for contrast");
        lcd.Write($"{i}");
        lcd.Contrast = i;
        Thread.Sleep(100);
    }

    Console.WriteLine("Reseting contrast to recommended value 40");
    lcd.Contrast = 40;
    lcd.Clear();

    Console.WriteLine("Testing the 4 different temperature modes");
    lcd.WriteLine("Test temp 0");
    lcd.Temperature = Iot.Device.Display.Pcd8544Enums.ScreenTemperature.Coefficient0;
    Thread.Sleep(1500);
    lcd.WriteLine("Test temp 1");
    lcd.Temperature = Iot.Device.Display.Pcd8544Enums.ScreenTemperature.Coefficient1;
    Thread.Sleep(1500);
    lcd.WriteLine("Test temp 2");
    lcd.Temperature = Iot.Device.Display.Pcd8544Enums.ScreenTemperature.Coefficient2;
    Thread.Sleep(1500);
    lcd.WriteLine("Test temp 3");
    lcd.Temperature = Iot.Device.Display.Pcd8544Enums.ScreenTemperature.Coefficient3;
    Thread.Sleep(1500);
    lcd.Temperature = Iot.Device.Display.Pcd8544Enums.ScreenTemperature.Coefficient0;
    lcd.Clear();

    Console.WriteLine("Adjusting bias from 0 to 6");
    // Adjusting the bias
    for (byte i = 0; i < 7; i++)
    {
        // Adjusting the bias
        lcd.Bias = i;
        lcd.SetCursorPosition(0, 0);
        lcd.WriteLine("Adjusting bias");
        lcd.Write($"bias = {i}");
        Thread.Sleep(2000);
        lcd.Clear();
    }

    Console.WriteLine("Reseting bias to recommended value 4");
    lcd.Bias = 4;
}

void DisplayTextChangePositionBlink()
{
    Console.WriteLine("Display is on and will switch on and off");
    lcd.Write("Display is on and will switch on and off");
    for (int i = 0; i < 5; i++)
    {
        Thread.Sleep(1000);
        lcd.Enabled = !lcd.Enabled;
    }

    lcd.Enabled = true;
    lcd.Clear();

    Console.WriteLine("Displaying multi line with WriteLine");
    lcd.SetCursorPosition(0, 0);
    lcd.Write("First line");
    lcd.SetCursorPosition(0, 1);
    lcd.Write("Second one");
    lcd.SetCursorPosition(0, 2);
    lcd.Write("3rd");
    lcd.SetCursorPosition(0, 3);
    lcd.Write("Guess!");
    lcd.SetCursorPosition(0, 4);
    lcd.Write("One more...");
    lcd.SetCursorPosition(0, 5);
    lcd.Write("last line");
    Thread.Sleep(1500);

    Console.WriteLine("Inverting the color screen");
    // this will blink the screen
    for (int i = 0; i < 6; i++)
    {
        lcd.InvertedColors = !lcd.InvertedColors;
        Thread.Sleep(1000);
    }

    Console.WriteLine("Activating the cursor, writting numbers in a raw");
    lcd.Clear();
    lcd.SetCursorPosition(0, 0);
    lcd.UnderlineCursorVisible = true;
    for (int i = 0; i < 50; i++)
    {
        lcd.Write($"{i}");
        Thread.Sleep(500);
    }

    lcd.Clear();
    Console.WriteLine("Testing backspace to remove character");
    lcd.Write("Basckspace");
    for (int i = 0; i < 5; i++)
    {
        Thread.Sleep(2000);
        lcd.Write("\b");
    }

    Console.WriteLine("Displaying more text and moving the cursor around");
    lcd.Clear();
    lcd.Write("More text");
    Thread.Sleep(1500);
    lcd.SetCursorPosition(0, 0);
    Thread.Sleep(1500);
    lcd.SetCursorPosition(5, 0);
    Thread.Sleep(1500);
    lcd.SetCursorPosition(0, 5);
    Thread.Sleep(1500);
    lcd.UnderlineCursorVisible = false;

    Console.WriteLine("This will display a line of random bits");
    lcd.Clear();
    lcd.WriteLine("This will display a line of random bits");
    Thread.Sleep(1500);
    byte[] textToSend = new byte[80];
    var rand = new Random(123456);
    rand.NextBytes(textToSend);
    lcd.Clear();
    lcd.SetCursorPosition(0, 0);
    lcd.Write(textToSend);
    Thread.Sleep(1000);
    lcd.Clear();
}

void DisplayingBitmap()
{
    Console.WriteLine("Displaying bitmap, text, resizing them");
    using Image<Rgba32> bitmapMe = (Image<Rgba32>)Image.Load(Path.Combine("me.bmp"));
    var bitmap1 = BitmapToByteArray(bitmapMe);
    using Image<Rgba32> bitmapNokia = (Image<Rgba32>)Image.Load(Path.Combine("nokia_bw.bmp"));
    var bitmap2 = BitmapToByteArray(bitmapNokia);

    // Open a non bitmap and resize it
    using Image<Rgba32> bitmapLarge = (Image<Rgba32>)Image.Load(Path.Combine("nonbmp.jpg"));
    bitmapLarge.Mutate(x => x.Resize(Pcd8544.PixelScreenSize));
    bitmapLarge.Mutate(x => x.BlackWhite());
    var bitmap3 = BitmapToByteArray(bitmapLarge);

    for (byte i = 0; i < 2; i++)
    {
        lcd.Clear();
        lcd.Write("    Bonjour     .NET IoT!");
        Thread.Sleep(2000);
        lcd.Clear();

        lcd.SetCursorPosition(0, 0);
        lcd.Write("This is me");
        Thread.Sleep(1000);
        // Shows the first bitmap
        lcd.SetByteMap(bitmap1);
        lcd.Draw();
        Thread.Sleep(1500);

        lcd.SetCursorPosition(0, 0);
        lcd.WriteLine("A nice");
        lcd.WriteLine("picture with");
        lcd.WriteLine("a heart");
        lcd.WriteLine("displayed");
        lcd.WriteLine("on the screen");
        Thread.Sleep(1000);
        // Shows the second bitmap
        lcd.SetByteMap(bitmap2);
        lcd.Draw();
        Thread.Sleep(1500);
        lcd.SetCursorPosition(0, 0);
        lcd.WriteLine("Large picture");
        lcd.WriteLine("resized and");
        lcd.WriteLine("changed to");
        lcd.WriteLine("monochrome");
        Thread.Sleep(1000);
        // Shows the second bitmap
        lcd.SetByteMap(bitmap3);
        lcd.Draw();
        Thread.Sleep(1500);
    }
}

void DisplayLinesPointsRectabngles()
{
    Console.WriteLine("Drawing point, line and rectangles");
    lcd.DrawPoint(5, 5, true);
    lcd.DrawLine(0, 0, 15, 35, true);
    lcd.DrawRectangle(10, 30, 10, 20, true, true);
    lcd.DrawRectangle(12, 32, 6, 16, false, true);
    // You should not forget to refresh to draw everything
    lcd.Draw();
    Thread.Sleep(2000);
    lcd.Clear();
    Console.WriteLine("Drawing 4 points at the 4 edges");
    lcd.DrawPoint(0, 0, true);
    lcd.DrawPoint(Pcd8544.PixelScreenSize.Width - 1, 0, true);
    lcd.DrawPoint(Pcd8544.PixelScreenSize.Width - 1, Pcd8544.PixelScreenSize.Height - 1, true);
    lcd.DrawPoint(0, Pcd8544.PixelScreenSize.Height - 1, true);
    lcd.Draw();
    Thread.Sleep(2000);
    Console.WriteLine("Drawing a rectangle at 2 pixels from the edge");
    lcd.DrawRectangle(2, 2, Pcd8544.PixelScreenSize.Width - 4, Pcd8544.PixelScreenSize.Height - 4, true, false);
    lcd.Draw();
    Thread.Sleep(2000);
    lcd.Clear();
    Console.WriteLine("Drawing 2 diagonal lines");
    lcd.DrawLine(0, 0, Pcd8544.PixelScreenSize.Width - 1, Pcd8544.PixelScreenSize.Height - 1, true);
    lcd.DrawLine(0, Pcd8544.PixelScreenSize.Height - 1, Pcd8544.PixelScreenSize.Width - 1, 0, true);
    lcd.Draw();
    Thread.Sleep(2000);
}

Console.WriteLine("Thank you for your attention!");
lcd.WriteLine("Thank you for your attention!");
Thread.Sleep(2000);
lcd.Dispose();
// In case we're using FT4222, gpio needs to be disposed after the screen
gpio?.Dispose();

byte[] BitmapToByteArray(Image<Rgba32> bitmap)
{
    if (bitmap is not object)
    {
        throw new ArgumentNullException(nameof(bitmap));
    }

    if ((bitmap.Width != Pcd8544.PixelScreenSize.Width) || (bitmap.Height != Pcd8544.PixelScreenSize.Height))
    {
        throw new ArgumentException($"{nameof(bitmap)} should be same size as the screen {Pcd8544.PixelScreenSize.Width}x{Pcd8544.PixelScreenSize.Height}");
    }

    byte[] toReturn = new byte[Pcd8544.ScreenBufferByteSize];
    int width = Pcd8544.PixelScreenSize.Width;
    Rgba32 colWhite = new(255, 255, 255);
    for (int position = 0; position < Pcd8544.ScreenBufferByteSize; position++)
    {
        byte toStore = 0;
        for (int bit = 0; bit < 8; bit++)
        {
            toStore = (byte)(toStore | ((bitmap[position % width, position / width * 8 + bit] == colWhite ? 0 : 1) << bit));
        }

        toReturn[position] = toStore;
    }

    return toReturn;
}
