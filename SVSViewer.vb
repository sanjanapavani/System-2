Imports Microsoft.VisualBasic
Imports System.Windows.Forms
Imports OpenSlideSharp
Imports System.Drawing.Imaging
Imports System.Runtime.InteropServices
Imports OpenSlideSharp.BitmapExtensions
Imports System

Public Class SVSViewer
    Inherits UserControl

    Private pictureBox As PictureBox

    Public Sub New(pictureBox As PictureBox)
        InitializeComponent()
        Me.pictureBox = pictureBox
        OpenSlideImage.Initialize()
    End Sub

    Public Sub LoadSVSImage(filePath As String)
        Try
            ' Open the SVS file
            Using slide As OpenSlideImage = OpenSlideImage.Open(filePath)
                ' Get the dimensions of the slide
                Dim width As Integer = slide.Dimensions.Width
                Dim height As Integer = slide.Dimensions.Height

                ' Read a region of the slide into a byte array
                Dim bitmap As Bitmap = slide.ReadRegionImage(0, 0, 0, width, height)

                ' Display the Bitmap in the PictureBox
                pictureBox.Image = bitmap
            End Using
        Catch ex As Exception
            MessageBox.Show("An error occurred while loading the SVS image: " & ex.Message)
        End Try
    End Sub

    Private Sub InitializeComponent()
        Me.SuspendLayout()
        ' 
        ' SVSViewer
        ' 
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.Name = "SVSViewer"
        Me.ResumeLayout(False)

    End Sub
End Class