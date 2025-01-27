Imports System.Runtime.InteropServices
Imports MathNet.Numerics
Public Class EO
    <DllImport("EO-Drive.dll")> Public Shared Function EO_InitHandle() As Integer
    End Function


    <DllImport("EO-Drive.dll")> Public Shared Function EO_InitAllHandles() As Integer
    End Function

    <DllImport("EO-Drive.dll")> Public Shared Function EO_GetAllHandles(ByRef handls As Integer(), size As Integer) As Integer
    End Function

    <DllImport("EO-Drive.dll")>
    Public Shared Function EO_NumberOfCurrentHandles() As Integer
    End Function
    <DllImport("EO-Drive.dll")>
    Public Shared Function EO_GetHandleBySerial(Serial As Short) As Integer
    End Function

    <DllImport("EO-Drive.dll")>
    Public Shared Sub EO_ReleaseHandle(handl As Integer)
    End Sub

    <DllImport("EO-Drive.dll")>
    Public Shared Sub EO_ReleaseAllHandles()
    End Sub

    'Device Movement & Information
    <DllImport("EO-Drive.dll")>
    Public Shared Function EO_Move(handl As Integer, position As Double) As Integer
    End Function

    <DllImport("EO-Drive.dll")>
    Public Shared Function EO_GetCommandPosition(handl As Integer, ByRef position As Double) As Integer
    End Function

    <DllImport("EO-Drive.dll")>
    Public Shared Function EO_GetSerialNumber(handl As Integer, ByRef serial As Integer) As Integer
    End Function
    Dim serial As Integer
    Dim err As Integer
    Dim last_position As Single
    Dim handl As Integer
    Dim sleep As Single
    Public initialDelay As Single
    Dim dir As Integer
    Dim steps As Integer
    Dim stage As Integer
    Dim a, b As Double
    Dim ticks As Long
    Dim current As Long
    Dim sx(100), sy(100) As Double
    Dim Spline As Interpolation.LinearSpline
    Public retrn As Boolean

    Public Sub New(steps As Integer)
        handl = EO_InitHandle()
        If (handl = 0) Then
            MsgBox("Failed To initialize the Piezo stage. check if it is powered ")
            Return
        End If
        err = EO_GetSerialNumber(handl, serial)

        'Set the piezo at the middle range
        MoveAbsolute(0)
        Me.steps = steps
        Try
            ReadS()
        Catch ex As Exception

        End Try
    End Sub

    Public Sub MoveRelative(ByVal position As Single)
        If position + last_position > 100 Or (position + last_position) < 0 Then Exit Sub
        EO_Move(handl, position + last_position)
        last_position = position + last_position
    End Sub

    Public Sub MoveAbsolute(ByVal position As Single)
        err = EO_Move(handl, position)
        last_position = position
    End Sub

    Public Sub Scan()

        For t = 1 To 10
            MakeDelay()
            MoveRelative(100 / steps)
        Next
        MakeDelay()
    End Sub


    Public Sub setSleep(exp As Single)

        Dim mn As Single = a * exp + b
        ticks = (mn * Stopwatch.Frequency / 1000)
        MakeDelay()
        MakeDelay()
    End Sub
    Public Sub Evaluate(steps As Integer)
        MoveAbsolute(0)
        Dim watch As New Stopwatch
        watch.Start()


        For t = 1 To steps
            MoveRelative(100 / steps)
        Next

        watch.Stop()
        MsgBox(watch.ElapsedMilliseconds)
        MoveAbsolute(0)
    End Sub

    Public Sub MakeDelay()

        current = System.Diagnostics.Stopwatch.GetTimestamp()

        While (System.Diagnostics.Stopwatch.GetTimestamp() - current) < ticks
            Application.DoEvents()
        End While

    End Sub
    Public Sub Calibrate(Pbar As ProgressBar)
        Pbar.Maximum = 100


        ReDim sx(100)
        ReDim sy(100)

        Pbar.Value = 0
        For mn = 0 To 100
            Threading.Thread.Sleep(100)
            MoveAbsolute(0)

            ticks = mn * Stopwatch.Frequency / 1000
            MakeDelay()
            MakeDelay()

            Pbar.Value = mn
            Dim watch As New Stopwatch
            watch.Start()
            Scan()
            watch.Stop()
            sx(mn) = watch.ElapsedMilliseconds
            sy(mn) = mn
            Pbar.Increment(1)
            Application.DoEvents()

        Next
        Pbar.Value = 0

        WriteS()
    End Sub

    Sub WriteS()
        Dim fn As Integer = FreeFile()
        FileOpen(fn, "Piezo.txt", OpenMode.Output)
        For i = 0 To sy.GetUpperBound(0)
            PrintLine(fn, sx(i), sy(i))
        Next
        Linearregression()

        FileClose(fn)
    End Sub

    Sub ReadS()
        Dim fn As Integer = FreeFile()
        FileOpen(fn, "Piezo.txt", OpenMode.Input)
        Dim i As Integer
        Do Until (EOF(fn))
            ReDim Preserve sx(i)
            ReDim Preserve sy(i)
            Input(fn, sx(i))
            Input(fn, sy(i))
            i += 1
        Loop
        FileClose(fn)
        Linearregression()
    End Sub
    Public Sub Linearregression()
        'Dim p As Tuple(Of Double, Double) = Fit.Line(sx, sy)
        'b = p.Item1
        'a = p.Item2
    End Sub
    Public Sub ShowPosition()
        'Form1.Label21.Text = EO_GetCommandPosition(handl, last_position)
    End Sub
End Class
