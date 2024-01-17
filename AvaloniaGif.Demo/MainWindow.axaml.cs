using System;
using Avalonia.Controls;

namespace AvaloniaGif.Demo;

public partial class MainWindow : Window
{
    private int i = 0;
    public MainWindow()
    {
        InitializeComponent();
        Image.Error += Delete;
        Image2.Error += Delete;
    }

    private void Delete()
    {
        i += 1;
        Console.WriteLine("Error" + i);
        Console.WriteLine("//");
        
    }
}