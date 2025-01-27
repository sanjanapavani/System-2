Imports PMDLibrary
Imports PMDLibrary.PMD
' start with -1000000 to go down. It is in zero. 1000000 is about 3 mm.
Public Class Dover
    Dim periph1 As PMDPeripheral
    Dim device1 As PMDDevice
    Public Axe As PMDAxis
    Dim Z As Integer
    Dim memory1 As PMDMemory
    Dim periph2 As PMDPeripheral
    Dim device2 As PMDDevice
    Dim axis2 As PMDAxis
    Dim Position As Integer
    Dim major, minor As UInt32
    Dim MPmajor, MPminor, NumberAxes, special, custom, family As UInt16
    Dim MotorType As PMDMotorTypeVersion
    Dim DeviceType As PMDDeviceType
    Const TCPTimeout = 1000
    Public Sub CloseAll()
        If (memory1 IsNot Nothing) Then
            memory1.Close()
        End If
        If (Axe IsNot Nothing) Then
            Axe.Close()
        End If
        If (device1 IsNot Nothing) Then
            device1.Close()
        End If
        If (periph1 IsNot Nothing) Then
            periph1.Close()
        End If
        If (axis2 IsNot Nothing) Then
            axis2.Close()
        End If
        If (device2 IsNot Nothing) Then
            device2.Close()
        End If
        If (periph2 IsNot Nothing) Then
            periph2.Close()
        End If
    End Sub

    Public Sub New()
        Dim ra1(0 To 10) As UInt32
        Dim i As UInt32
        Dim IPAddr As UInt32

        DeviceType = PMDDeviceType.MotionProcessor
        Try
            '57600
            periph1 = New PMDPeripheralCOM(3, 57600, PMDSerialParity.None, PMDSerialStopBits.SerialStopBits1)
            device1 = New PMDDevice(periph1, DeviceType)
            Axe = New PMDAxis(device1, PMDAxisNumber.Axis1)
            Axe.GetVersion(family, MotorType, NumberAxes, special, custom, MPmajor, MPminor)

            If (DeviceType = PMDDeviceType.ResourceProtocol) Then
                device1.Version(major, minor)
                IPAddr = device1.GetDefault(PMDDefault.IPAddress)
                memory1 = New PMDMemory(device1, PMDDataSize.Size32Bit, PMDMemoryType.DPRAM)
                For i = 0 To ra1.Length() - 1
                    ra1(i) = i

                Next
                memory1.Write(ra1, 100, ra1.Length())
            End If

            Axe.ActualPositionUnits = PMD.PMDActualPositionUnits.Counts


            'homing
            'Axe.SetEventAction(1, 0)
            'Axe.SetEventAction(2, 0)
            Axe.ResetEventStatus(40960)
            Axe.ResetEventStatus(61439)
            Axe.OperatingMode = 55

            Axe.ActualPositionUnits = PMD.PMDActualPositionUnits.Counts

            Axe.Update()

        Catch ex As Exception
            MsgBox(ex.Message)
        End Try

    End Sub
    Public Sub Home()
        MoveAbsolute(0, 0)
    End Sub

    Public Sub SetAcceleration(A As Integer)
        Axe.Acceleration = A
        Axe.Update()
    End Sub

    Public Function GetPosition(Unit As Integer)
        Return Counts_to_MM(Axe.Position)
    End Function
    Public Sub SetSpeed(S As Integer)
        Axe.Velocity = S
        Axe.Update()
    End Sub
    Public Sub MoveRelative(Z As Single, Units As Integer)
        Position -= MM_to_Counts(Z)
        Axe.Position = Position
        Axe.Update()
        WaitAxis()
    End Sub
    Public Sub MoveRelativeAsync(Z As Single, Units As Integer)
        WaitAxis()
        Position -= MM_to_Counts(Z)
        Axe.Position = Position
        Axe.Update()


    End Sub
    Public Sub MoveAbsolute(Z As Integer, Units As Integer)
        Position = -MM_to_Counts(Z)
        Axe.Position = Position
        Axe.Update()
        WaitAxis()
    End Sub

    Public Sub MoveAbsoluteAsync(Z As Single, Units As Integer)
        Position = -MM_to_Counts(Z)
        Axe.Position = Position
        Axe.Update()

    End Sub


    Public Sub WaitAxis()
        Do

        Loop Until Mid(ToBinary(Axe.ActivityStatus), 13, 1) = 0
    End Sub

    Function Counts_to_MM(Counts As Integer) As Single
        Return Counts * 3 / 1000000
    End Function
    Function MM_to_Counts(MM As Single) As Integer
        Dim counts As Integer = MM * 1000000 / 3
        If counts > 1000000 Then counts = 1000000
        Return counts
    End Function
    Private Function ToBinary(dec As Integer) As String
        Dim bin As Integer
        Dim output As String
        While dec <> 0
            If dec Mod 2 = 0 Then
                bin = 0
            Else
                bin = 1
            End If
            dec = dec \ 2
            output = Convert.ToString(bin) & output
        End While
        If output Is Nothing Then
            Return "0"
        Else
            Return output
        End If
    End Function

End Class
