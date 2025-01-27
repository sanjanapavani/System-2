Imports System.Drawing.Imaging
Imports Accord.Imaging.Converters

Public Class FastBMP

    Public Structure ROIStructure
        Public Area As Rectangle
        Public selected As Boolean

    End Structure


    Public bmp As Bitmap
    Public width As Integer
    Public height As Integer
    Public bytes() As Byte
    Private byteCopy() As Byte
    Public GR As Graphics

    Public ROI() As Rectangle
    Public ROIindex As Integer
    Public ROISelected() As Boolean



    Public Sub New(W As Integer, H As Integer, fmt As PixelFormat)
        bmp = New Bitmap(W, H, fmt)
        width = W
        height = H
        GR = Graphics.FromImage(bmp)
        ReDim bytes(W * H * 4 - 1)
        ReDim byteCopy(W * H * 4 - 1)
        ResetROI()
    End Sub

    Public Sub MakeNewFromBytes(W As Integer, H As Integer, fmt As PixelFormat)
        bmp = New Bitmap(W, H, fmt)
        width = W
        height = H
        byteToBitmap(bytes, bmp)
        GR = Graphics.FromImage(bmp)

    End Sub

    Public Sub RefreshROI()
        Reset()
        For i = 0 To ROI.GetUpperBound(0)
            GR.DrawRectangle(New Pen(Color.AliceBlue), ROI(i))
        Next
    End Sub


    Public Sub DrawROI()

        For i = 0 To ROI.GetUpperBound(0)
            GR.DrawRectangle(New Pen(Color.AliceBlue), ROI(i))
        Next
    End Sub



    Public Sub ResetROI()
        ReDim ROI(0)
        ROI(0) = New Rectangle(0, 0, width, height)
        ROIindex = 0
    End Sub
    Public Sub Reset()
        byteToBitmap(bytes, bmp)
    End Sub

    Public Sub Unlock()
        ReDim byteCopy(bytes.GetUpperBound(0))
        Array.Copy(bytes, byteCopy, bytes.GetLength(0))
        BitmapToBytes(bmp, byteCopy)
    End Sub

    Public Sub SetPixel(x As Integer, y As Integer, clr As Color)
        Dim index As Integer = (y * width + x) * 4
        If index <= ((height * width) * 4 - 1) Then
            byteCopy(index) = clr.R
            byteCopy(index + 1) = clr.G
            byteCopy(index + 2) = clr.B
            byteCopy(index + 3) = clr.A
        End If
    End Sub

    Public Sub SetPixel(x As Integer, y As Integer, byteR As Byte, byteG As Byte, byteB As Byte, byteA As Byte)
        Dim index As Integer = (y * width + x) * 4
        byteCopy(index) = byteR
        byteCopy(index + 1) = byteG
        byteCopy(index + 2) = byteB
        byteCopy(index + 3) = byteA
    End Sub

    Public Sub FillOriginalPixel(x As Integer, y As Integer, byteR As Byte, byteG As Byte, byteB As Byte, byteA As Byte)
        Dim index As Integer = (y * width + x) * 4
        bytes(index) = byteR
        bytes(index + 1) = byteG
        bytes(index + 2) = byteB
        bytes(index + 3) = byteA
    End Sub




    Public Sub lock()
        byteToBitmap(byteCopy, bmp)
    End Sub

End Class





Module Bitmaps
    Public Sub byteToBitmap(bytes() As Byte, bmp As Bitmap)
        Dim rect As Rectangle = New Rectangle(0, 0, bmp.Width, bmp.Height)
        Dim bmpData As BitmapData = bmp.LockBits(rect, ImageLockMode.ReadWrite, bmp.PixelFormat)
        ' Copy the RGB values back to the bitmap
        Runtime.InteropServices.Marshal.Copy(bytes, 0, bmpData.Scan0, bmpData.Stride * bmp.Height)
        ' Unlock the bits.
        bmp.UnlockBits(bmpData)
        If bmp.PixelFormat = PixelFormat.Format8bppIndexed Then
            bmp.Palette = GetGrayScalePalette()
        End If
    End Sub

    Public Function BitmapToBytes(bmp As Bitmap, ByRef bytes() As Byte) As Integer
        Dim rect As Rectangle = New Rectangle(0, 0, bmp.Width, bmp.Height)
        Dim bmpData As BitmapData = bmp.LockBits(rect, ImageLockMode.ReadWrite, bmp.PixelFormat)
        ' Copy the RGB values back to the bitmap
        ReDim bytes(bmpData.Stride * bmp.Height - 1)
        Runtime.InteropServices.Marshal.Copy(bmpData.Scan0, bytes, 0, bmpData.Stride * bmp.Height)
        ' Unlock the bits.
        bmp.UnlockBits(bmpData)
        Return bmpData.Stride
    End Function

    Public Function BitmapToJaggedArray(bmp As Bitmap) As Double()()
        Dim imageToArray As New ImageToArray(min:=-1, max:=+1)
        ' Transform the image into an  jagged array of pixel values
        Dim pixels As Double()()
#Disable Warning BC42030 ' Variable is passed by reference before it has been assigned a value
        imageToArray.Convert(bmp, pixels)
#Enable Warning BC42030 ' Variable is passed by reference before it has been assigned a value
        Return pixels
    End Function

    Public Sub SetPixelByte32(ByRef bytes() As Byte, X As Integer, Y As Integer, width As Integer, clr As Color)
        Dim index As Integer = (Y * width + X) * 4
        bytes(index) = clr.B
        bytes(index + 1) = clr.G
        bytes(index + 2) = clr.R
        bytes(index + 3) = clr.A

    End Sub

    Public Sub SetPixelByte32Linear(ByRef bytes() As Byte, index As Integer, clr As Color)
        bytes(index) = clr.B
        bytes(index + 1) = clr.G
        bytes(index + 2) = clr.R
        bytes(index + 3) = clr.A
    End Sub
    Private Function GetGrayScalePalette() As ColorPalette
        Dim bmp As Bitmap = New Bitmap(1, 1, Imaging.PixelFormat.Format8bppIndexed)
        Dim monoPalette As ColorPalette = bmp.Palette
        Dim entries() As Color = monoPalette.Entries

        Dim i As Integer
        For i = 0 To 256 - 1 Step i + 1
            entries(i) = Color.FromArgb(i, i, i)
        Next

        Return monoPalette
    End Function





End Module
