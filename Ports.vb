Imports System.IO
Imports System.IO.Ports
Imports System.Management

Module Ports

    Private Declare Function FT_Open Lib "FTD2XX.dll" (ByVal intDeviceNumber As Integer, ByRef lnghandle As Long) As Long
    Private Declare Function FT_Close Lib "FTD2XX.dll" (ByVal lnghandle As Long) As Long
    Private Declare Function FT_ListDevices Lib "FTD2XX.dll" (ByVal arg1 As Long, ByVal arg2 As String, ByVal dwFlags As Long) As Long
    Public Function GetPortIDs() As String()
        Dim processClass As ManagementClass = New ManagementClass("Win32_PnPEntity")
        Dim Ports As ManagementObjectCollection = processClass.GetInstances()
        Dim h() As String
        Dim b As Integer
        For Each [property] As ManagementObject In Ports
            Dim name = [property].GetPropertyValue("Name")

            If name IsNot Nothing AndAlso name.ToString().Contains("USB") AndAlso name.ToString().Contains("COM") Then
                ReDim Preserve h(b)
                h(b) = [property].GetPropertyValue("PNPDeviceID")
                b += 1
            End If
        Next
        Return h
    End Function

    Public Function GetPortNames()
        'Return SerialPort.GetPortNames
        Dim processClass As ManagementClass = New ManagementClass("Win32_PnPEntity")
        Dim Ports As ManagementObjectCollection = processClass.GetInstances()
        Dim h() As String
        Dim b As Integer
        For Each [property] As ManagementObject In Ports
            Dim name = [property].GetPropertyValue("Name")

            If name IsNot Nothing AndAlso name.ToString().Contains("USB") AndAlso name.ToString().Contains("COM") Then
                ReDim Preserve h(b)
                h(b) = [property].GetPropertyValue("Caption").split("(")(1).split(")")(0)
                b += 1
            End If
        Next
        Return h
    End Function

    Public Sub ResetPorts()

        Dim arg1 As Long
        Dim arg2 As String = ""
        Dim dwFlag As Long

        Dim List As Long = FT_ListDevices(arg1, arg2, dwFlag)
        Dim lnghandle As Long

        For i = 0 To List - 1
            FT_Open(i, lnghandle)
            FT_Close(lnghandle)
        Next

    End Sub

    Private Sub SurroundingSub()
        Dim Availability As Integer
        Dim Caption As String
        Dim ClassGuid As String
        Dim CompatibleID As String()
        Dim ConfigManagerErrorCode As Integer
        Dim ConfigManagerUserConfig As Boolean
        Dim CreationClassName As String
        Dim Description As String
        Dim DeviceID As String
        Dim ErrorCleared As Boolean
        Dim ErrorDescription As String
        Dim HardwareID As String()
        Dim InstallDate As DateTime
        Dim LastErrorCode As Integer
        Dim Manufacturer As String
        Dim Name As String
        Dim PNPClass As String
        Dim PNPDeviceID As String
        Dim PowerManagementCapabilities As Integer()
        Dim PowerManagementSupported As Boolean
        Dim Present As Boolean
        Dim Service As String
        Dim Status As String
        Dim StatusInfo As Integer
        Dim SystemCreationClassName As String
        Dim SystemName As String
    End Sub
End Module
