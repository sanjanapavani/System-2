Imports Zaber.Motion
Imports Zaber.Motion.Binary

Public Class Zaber_Dover

    Dim Com As Object
    Public Elapsedtime As Long
    Dim Watch As New Stopwatch
    Public X, Y, Z As Single
    Public Xacc, Yacc, Zacc As Single
    Public Xspeed, Yspeed, Zspeed As Single
    Public xp, yp As Single
    Public FOVX, FOVY As Single
    Public SweptZ As Single
    Public Xaxe, Yaxe As Device
    Public Zaxe As Dover

    Public Sub New(FOVX As Single, FOVY As Single)
        Dim com As Connection = Connection.OpenSerialPort("COM21")
        Dim Devicelist = com.DetectDevices()
        Xaxe = Devicelist(0)
        Yaxe = Devicelist(1)

        Me.FOVX = FOVX
        Me.FOVY = FOVY
        Zaxe = New Dover
        Home()
        MoveAbsolute(Xaxe, 11.7, False)
        MoveAbsolute(Yaxe, 37.6, False)
    End Sub

    Public Sub SetFOV(FOVX As Single, FOVY As Single)
        Me.FOVX = FOVX
        Me.FOVY = FOVY
        Setting.Sett("FOVX", FOVX)
        Setting.Sett("FOVY", FOVY)
    End Sub

    Public Sub Home()
        Zaxe.Home()
        Xaxe.Home()
        Yaxe.Home()

        'MoveAbsolute(Zaxe, Setting.Gett("ZOFFSET"))
        'StorePosition(Zaxe, 1)

        'MoveAbsolute(Zaxe, Setting.Gett("FOCUS"))
        'StorePosition(Zaxe, 2)

        Xspeed = 65
        Yspeed = 65
        Zspeed = 100000000


        SetSpeed(Xaxe, Xspeed)
        SetSpeed(Yaxe, Yspeed)
        Zaxe.SetSpeed(Zspeed)


        Xacc = 3000
        Yacc = 3000
        Zacc = 10000

        SetAcceleration(Xaxe, Xacc)
        SetAcceleration(Yaxe, Yacc)
        Zaxe.SetAcceleration(Zacc)


    End Sub
    Public Sub MoveRelative(ByRef Axis As Object, R As Single, Optional update As Boolean = True)

        Try

            Axis.MoveRelative(R, Units.Length_Millimetres)
            If update Then
                UpdatePositions()
                Tracking.Update()
            End If
        Catch ex As Exception

        End Try

    End Sub

    Public Sub MoveRelativeAsync(ByRef Axis As Object, R As Single, Optional update As Boolean = True)


        Axis.MoveRelativeAsync(R, Units.Length_Millimetres)


        If update Then
            UpdatePositions()
            Tracking.Update()
        Else

        End If
    End Sub
    Public Sub MoveAbsolute(ByRef Axis As Object, R As Single, Optional update As Boolean = True)
        Try

            Axis.MoveAbsolute(R, Units.Length_Millimetres)

            If update Then
                UpdatePositions()
                Tracking.Update()
            End If
        Catch ex As Exception

        End Try

    End Sub

    Public Sub MoveAbsoluteAsync(ByRef Axis As Object, R As Single)
        Try
            Axis.MoveAbsoluteAsync(R, Units.Length_Millimetres)
            UpdatePositions()
            Tracking.Update()
        Catch ex As Exception

        End Try

    End Sub
    Public Sub SetSpeed(ByRef Axis As Object, S As Single)
        Try
            If Axis Is Zaxe Then
                Zaxe.SetSpeed(S)
            Else
                Axis.GenericCommandWithUnits(42, S, Units.Velocity_MillimetresPerSecond, Units.Velocity_MillimetresPerSecond, 100)
            End If
        Catch ex As Exception

        End Try

    End Sub

    Public Sub SetAcceleration(ByRef Axis As Object, A As Integer)
        If Axis Is Zaxe Then
            Zaxe.SetAcceleration(A)
        Else
            Axis.GenericCommand(43, A, 100, True)
        End If

    End Sub

    Public Function GetPosition(ByRef Axis As Object) As Single
        Return Axis.GetPosition(Units.Length_Millimetres)
    End Function

    Public Sub StorePosition(ByRef Axis As Device, position As Integer)
        Axis.GenericCommand(16, position, 0, True)
    End Sub


    Public Sub GoZero(ByRef Axis As Dover, block As Boolean)
        Try

            Dim ZZ As String = Setting.Gett("ZOFFSET")
            If block Then
                MoveAbsolute(Zaxe, ZZ - 5, True)
            Else
                MoveAbsolute(Zaxe, ZZ, True)
            End If


        Catch ex As Exception

        End Try

    End Sub

    Public Sub GoToFocus(block As Boolean)
        Try

            Dim ZZ As String = Setting.Gett("Focus")
            If block Then
                MoveAbsolute(Zaxe, ZZ - 5, True)
            Else
                MoveAbsolute(Zaxe, ZZ, True)
            End If

        Catch ex As Exception

        End Try

    End Sub

    Public Sub UpdatePositions()
        X = Xaxe.GetPosition(Units.Length_Millimetres)
        Y = Yaxe.GetPosition(Units.Length_Millimetres)
        Z = Zaxe.GetPosition(Units.Length_Millimetres)
    End Sub

    Public Sub UpdateZPositions()
        Z = Zaxe.GetPosition(Units.Length_Millimetres)
    End Sub

    Public Sub CalibrateZoffset(AutoFocusrange As Single)
        Stage.MoveRelative(Stage.Zaxe, -AutoFocusrange / 2)
        Dim ZZ As Single = Stage.GetPosition(Stage.Zaxe)
        Setting.Sett("ZOFFSET", ZZ)
        'StorePosition(Stage.Zaxe, 1)
        Stage.MoveRelative(Stage.Zaxe, AutoFocusrange / 2)
        ZZ = Stage.GetPosition(Stage.Zaxe)
        Setting.Sett("Focus", ZZ)
        'StorePosition(Stage.Zaxe, 2)
    End Sub

    Public Sub Go_Middle()
        MoveAbsolute(Xaxe, 12.7)
        MoveAbsolute(Yaxe, 38)
    End Sub

    Public Sub SetSweptZ(SweptZ As Single)
        Me.SweptZ = SweptZ
    End Sub


End Class
