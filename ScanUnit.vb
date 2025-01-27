Imports System.IO

Public Class ScanUnit

    Public Zscan As ZstackStructure
    Public done As Boolean
    Dim Myb As Integer

    Dim X, Y As Integer
    Dim FileX, FileY As Integer
    Dim W, H As Integer
    Dim Range, stepsize As Single
    Dim scale As Integer
    Dim Address As String
    Dim SaveThread As System.Threading.Thread
    Dim ProcessSingleThread As System.Threading.Thread
    Dim bmp As Bitmap
    Dim Dehaze As DehazeClass
    Dim Directory As String
    Dim Bytetest() As Byte
    Dim FileName As String



    Public Sub New(W As Integer, H As Integer, Range As Single, Stepsize As Single, scale As Integer, ID As Integer)
        Me.Range = Range
        Me.stepsize = Stepsize
        Me.scale = scale
        Me.Myb = ID
        Me.W = W
        Me.H = H
        done = True
        bmp = New Bitmap(W, H, Imaging.PixelFormat.Format24bppRgb)
        ReDim Bytetest(W * H * 3 - 1)
        Zscan = New ZstackStructure(W, H, Range, Stepsize, scale)
        Dehaze = New DehazeClass(W, H, 0.008, 0.5)

    End Sub
    Public Sub InputSettings(X As Integer, Y As Integer, Directory As String, Filename As String)
        Me.X = X
        Me.Y = Y
        Me.Address = Address
        Me.Directory = Directory
        Me.FileName = Filename
    End Sub

    Public Sub Acquire2(loop_x As Integer, loop_y As Integer, Hdirection As Integer, Vdirection As Integer, EDOF As Boolean)
        Stage.WaitUntilIdle(Stage.Zaxe)
        Do Until done
            Application.DoEvents()
        Loop
        done = False
        If Hdirection = 1 Then FileY = loop_y - 1 Else FileY = Y - loop_y
        FileX = loop_x - 1
        SaveThread = New System.Threading.Thread(AddressOf Save)
        Zscan.PrepareAcquire(False, Vdirection, Myb)
        SaveThread.Start()

        Do Until Not Zscan.ActivelyCapturing

        Loop
    End Sub




    Public Sub Acquire(loop_x As Integer, loop_y As Integer, Hdirection As Integer, Vdirection As Integer, EDOF As Boolean)

        Do Until done
            Application.DoEvents()
        Loop
        done = False
        If Hdirection = 1 Then FileY = loop_y - 1 Else FileY = Y - loop_y
        FileX = loop_x - 1
        'Threading.Thread.Sleep(100)


        If EDOF Then
            Zscan.Acquire(False, Vdirection)
        Else

            ReDim Zscan.OutputBytes(Camera.W * Camera.H * 3 - 1)
            Camera.Capture(Zscan.OutputBytes)
            Zscan.WrapUpDone = True
        End If

        'Save()

        Dim SaveThread As New System.Threading.Thread(AddressOf Save)
        SaveThread.Start()
        'Stage.WaitUntilIdle(Stage.Zaxe)
    End Sub

    Public Sub Save()
        Do Until Zscan.WrapUpDone
            Application.DoEvents()
        Loop
        Dehaze.Apply(Zscan.OutputBytes)
        byteToBitmap(Zscan.OutputBytes, bmp)
        bmp.Save(Path.Combine(Directory, FileName + " Y" + FileY.ToString("D5") + " X" + FileX.ToString("D5") + ".bmp"))
        'SaveJaggedArray(Zscan.bytes, W, H, Path.Combine(Directory, FileName + " Y" + FileY.ToString("D5") + " X" + FileX.ToString("D5") + ".tif"))
        done = True
        Application.DoEvents()
    End Sub

End Class
