'Imports Zaber.Serial.Core

'Public Class Zaber
'    Public X As Single
'    Public Y As Single
'    Public Z As Single
'    Public xx, yy As Integer
'    'reference point for focusing 
'    Public xp, yp As Single
'    Public Speed As Single
'    Public ShowTrace As Boolean
'    Dim TraceWidth As Integer
'    Dim TraceHeight As Integer
'    Dim posReply As BinaryReply
'    Dim XRange As Single
'    Dim YRange As Single
'    Dim ZRange As Single
'    Public Xcorrection, YCorrection As Single
'    Dim ResXcorrection, Resycorrection As Single
'    Public Z1, Z2, Z3 As Single
'    Dim BmpTrace As Bitmap
'    Dim GrossBitmap As Bitmap
'    Dim picture As Bitmap
'    Dim gr_picture As Graphics
'    Dim gr As Graphics
'    Dim Dx, Dy As Integer
'    Public TraceScale As Single
'    Public FovX, FovY As Single
'    Public correct As Boolean
'    Dim name As String
'    Public Zport, Xport, Yport, Allport As Integer
'    Public status As Boolean
'    Public MMtoSteps, ZMMtoSteps As Integer
'    Dim com As Object


'    Public Sub New(Speedin As Single, FovXin As Single, FovYin As Single)
'        com = New ZaberBinaryPort("COM5")
'        status = False
'        ' Try
'        com.open()
'        name = "Zaber"
'        MMtoSteps = 5246
'        ZMMtoSteps = 10100

'        XRange = 25 * MMtoSteps
'        YRange = 25 * MMtoSteps
'        ZRange = 130000

'        FovX = FovXin
'        FovY = FovYin

'        Zport = 3
'        Xport = 1
'        Yport = 2
'        Allport = 4


'        Dim sp As Integer = Setting.Gett("SPEED5X")

'        SetSpeed(Allport, sp, 200)
'        'SetSpeed(Yport, sp)
'        'SetSpeed(Zport, sp)

'        Home()

'        Xcorrection = Setting.Gett("XCORRECTION")
'        YCorrection = Setting.Gett("YCORRECTION")

'        status = True

'        'sp = Setting.Gett("SPEED1X")
'        'SetSpeed(Allport, sp, 0)

'    End Sub
'    Public Sub close(ByVal port As String)
'        com.Close()
'    End Sub

'    Public Sub SetFOV(fov_x As Single, fov_y As Single)
'        FovX = fov_x
'        FovY = fov_y
'    End Sub

'    Public Sub Home()


'        Dim moveAxis3 = New BinaryCommand(Zport, 1, 0)
'        com.Write(moveAxis3)
'        com.read()

'        moveAxis3 = New BinaryCommand(Zport, 20, Setting.Gett("ZOFFSET") * ZMMtoSteps)
'        com.Write(moveAxis3)
'        posReply = com.read()
'        Z = posReply.Data



'        'stores the position 
'        com.write(New BinaryCommand(Zport, 16, 0))
'        com.read



'        Dim moveAxis1 = New BinaryCommand(Xport, 1, 0)
'        com.Write(moveAxis1)
'        com.read()
'        moveAxis1 = New BinaryCommand(Xport, 20, XRange / 2)
'        com.Write(moveAxis1)
'        posReply = com.read()
'        X = posReply.Data



'        Dim moveAxis2 = New BinaryCommand(Yport, 1, 0)
'        com.Write(moveAxis2)
'        com.read()
'        moveAxis2 = New BinaryCommand(Yport, 20, YRange / 2)
'        com.Write(moveAxis2)
'        posReply = com.read()
'        Y = posReply.Data




'    End Sub
'    Public Sub StorePosition()
'        com.write(New BinaryCommand(Zport, 16, 0))
'        posReply = com.read
'        'Setting.Sett("Zoffset", posReply.Data)
'    End Sub

'    Public Sub Go_Middle()
'        Dim moveAxis1 = New BinaryCommand(Xport, 20, XRange / 2)
'        com.Write(moveAxis1)
'        posReply = com.read()
'        X = posReply.Data

'        Dim moveAxis2 = New BinaryCommand(Yport, 20, YRange / 2)
'        com.Write(moveAxis2)
'        posReply = com.read()
'        Y = posReply.Data

'        Tracking.Update()
'    End Sub

'    Public Sub GoZero(position As Integer)
'        'goes to the stored position
'        Select Case position
'            Case 1
'                com.write(New BinaryCommand(Zport, 18, 0))
'                com.read
'            Case 2
'                com.write(New BinaryCommand(Zport, 18, 1))
'                com.read
'        End Select

'    End Sub

'    Public Sub SetSpeed(ByVal axis As Integer, ByVal speedin As Integer, ByVal accelIn As Integer)

'        If accelIn = 0 Then accelIn = 100 'Default value

'        Select Case axis
'            Case Xport
'                Dim VA = New BinaryCommand(Xport, 42, speedin)
'                com.Write(VA)
'                com.read()

'                VA = New BinaryCommand(Xport, 43, accelIn)
'                com.Write(VA)
'                com.read()
'            Case Yport

'                Dim VA2 = New BinaryCommand(Yport, 42, speedin)
'                com.Write(VA2)
'                com.read()

'                VA2 = New BinaryCommand(Yport, 43, accelIn)
'                com.Write(VA2)
'                com.read()
'            Case Zport

'                Dim VA3 = New BinaryCommand(Zport, 42, speedin)
'                com.Write(VA3)
'                com.read()

'                VA3 = New BinaryCommand(Zport, 43, accelIn)
'                com.Write(VA3)
'                com.read()

'            Case Allport
'                Dim VA = New BinaryCommand(Xport, 42, speedin)
'                com.Write(VA)
'                com.read()

'                VA = New BinaryCommand(Xport, 43, accelIn)
'                com.Write(VA)
'                com.read()

'                Dim VA2 = New BinaryCommand(Yport, 42, speedin)
'                com.Write(VA2)
'                com.read()

'                VA2 = New BinaryCommand(Yport, 43, accelIn)
'                com.Write(VA2)
'                com.read()

'                Dim VA3 = New BinaryCommand(Zport, 42, speedin)
'                com.Write(VA3)
'                com.read()

'                VA3 = New BinaryCommand(Zport, 43, accelIn)
'                com.Write(VA3)
'                com.read()
'        End Select

'        Speed = speedin
'    End Sub


'    Public Sub Move_r(ByVal axis As Integer, ByVal position As Single)

'        Select Case axis

'            Case Yport

'                Dim moveAxis1 = New BinaryCommand(Yport, 21, position * MMtoSteps)
'                com.Write(moveAxis1)
'                posReply = com.read()
'                Y = posReply.Data



'            Case Xport


'                Dim moveAxis2 = New BinaryCommand(Xport, 21, -position * MMtoSteps)
'                com.Write(moveAxis2)
'                posReply = com.read()
'                X = posReply.Data


'            Case Zport

'                Dim moveAxis3 = New BinaryCommand(Zport, 21, position * ZMMtoSteps)
'                com.Write(moveAxis3)
'                posReply = com.read
'                Z = posReply.Data
'        End Select

'        Tracking.Update()

'    End Sub





'    Public Sub Move_A(ByVal axis As Integer, ByVal position As Single)

'        Select Case axis

'            Case Xport

'                'If correct Then
'                '    correction = (position - GetPosition(Xport)) * Xcorrection * ZMMtoSteps
'                '    Dim moveAxis3 = New BinaryCommand(Zport, 21, -correction)
'                '    com.Write(moveAxis3)
'                '    Z = com.read()

'                'End If

'                Dim moveAxis1 = New BinaryCommand(Xport, 20, position * MMtoSteps)
'                com.Write(moveAxis1)
'                posReply = com.read()
'                X = posReply.Data


'            Case Yport

'                'If correct Then
'                '    correction = (position - GetPosition(Yport)) * YCorrection * ZMMtoSteps
'                '    Dim moveAxis3 = New BinaryCommand(Zport, 21, -correction)
'                '    com.Write(moveAxis3)
'                '    Z = com.read()
'                'End If

'                Dim moveAxis2 = New BinaryCommand(Yport, 20, position * MMtoSteps)
'                com.Write(moveAxis2)
'                posReply = com.read()
'                Y = posReply.Data

'            Case Zport
'                Dim moveAxis3 = New BinaryCommand(Zport, 20, position * ZMMtoSteps)
'                com.Write(moveAxis3)
'                posReply = com.read()
'                Z = posReply.Data
'        End Select
'        Tracking.Update()
'    End Sub
'    Public Sub STP(ByVal axis As Integer)
'        Select Case axis

'            Case 1
'                Dim moveAxis1 = New BinaryCommand(Xport, 23, 0)
'                com.Write(moveAxis1)
'                com.read()
'            Case 2
'                Dim moveAxis2 = New BinaryCommand(Yport, 23, 0)
'                com.Write(moveAxis2)
'                com.read()
'            Case 3

'        End Select



'    End Sub

'    Public Function GetPosition(axis As Integer) As Single
'        Dim pos As Single
'        Select Case axis
'            Case Yport
'                Dim moveAxis1 = New BinaryCommand(Yport, 60, 0)
'                com.Write(moveAxis1)
'                posReply = com.read()
'                pos = posReply.Data / MMtoSteps
'            Case Xport
'                Dim moveAxis2 = New BinaryCommand(Xport, 60, 0)
'                com.Write(moveAxis2)
'                posReply = com.read()
'                pos = posReply.Data / MMtoSteps
'            Case Zport
'                Dim moveAxis3 = New BinaryCommand(Zport, 60, 0)
'                com.Write(moveAxis3)
'                posReply = com.read()
'                pos = posReply.Data / ZMMtoSteps
'        End Select
'        Return pos
'    End Function

'End Class
