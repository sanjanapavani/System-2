Imports System.Text
Imports System.Threading
Imports System.Windows.Forms.VisualStyles.VisualStyleElement.ToolBar
Imports Thorlabs.TLUP_64.Interop

Public Class LEDStructure
    Public Structure ConnectedDeviceInfo
        Public Manufacturer As String
        Public ModelName As String
        Public SerialNumber As String
        Public ResourceName As String
        Public IsAvailable As Boolean

        Public Name As String
        Public FirmwareRevision As String
    End Structure

    Public connectedDeviceInfo_list As New Dictionary(Of UInt32, ConnectedDeviceInfo)
    Public selectedDeviceInfo As ConnectedDeviceInfo = Nothing
    Public upDevice As TLUP = Nothing
    Public ReadOnly throwErrors As Boolean = False


    Public Sub New()
        SearchAndConnectDevice(0)
    End Sub

    Public Sub TurnOn()
        Try
            Dim retCode As Integer = CInt(TLUP_RETURN_CODES.Success)
            Dim outputEnabled As Boolean = True
            retCode = upDevice.Led_EnableOutput(outputEnabled)

            If retCode <> CInt(TLUP_RETURN_CODES.Success) Then
                Throw New Exception("Error turning on LED")
            End If
        Catch ex As Exception
            MessageBox.Show("Error turning on LED: " + ex.Message)
        End Try
    End Sub

    Public Sub TurnOff()
        Try
            Dim retCode As Integer = CInt(TLUP_RETURN_CODES.Success)
            Dim outputEnabled As Boolean = False
            retCode = upDevice.Led_EnableOutput(outputEnabled)
            If retCode <> CInt(TLUP_RETURN_CODES.Success) Then
                Throw New Exception("Error turning off LED")
            End If
        Catch ex As Exception
            MessageBox.Show("Error turning off LED: " + ex.Message)
        End Try
    End Sub



    Public Sub SetLEDCurrent(currentLimit As Double)
        Try
            Dim retCode As Integer = CInt(TLUP_RETURN_CODES.Success)

            ' Assuming the currentLimit is in the correct range. If there's a specific range, you might want to check and constrain the value here.

            ' Set the current limit
            retCode = upDevice.Led_SetCurrentSetpoint(currentLimit)

            If retCode <> CInt(TLUP_RETURN_CODES.Success) Then
                Throw New Exception("Error setting LED current limit")
            End If
        Catch ex As Exception
            MessageBox.Show("Error setting LED current limit: " + ex.Message)
        End Try
    End Sub

    Public Sub SearchAndConnectDevice(ByVal deviceSelection As UInt32)
        Dim retCode As Int32 = CInt(TLUP_RETURN_CODES.Success)

        Dim deviceModelName As New StringBuilder(TLUP.StringLength)
        Dim deviceSerialNumber As New StringBuilder(TLUP.StringLength)
        Dim deviceManufacturer As New StringBuilder(TLUP.StringLength)
        Dim deviceResourceName As New StringBuilder(TLUP.StringLength)
        Dim deviceName As New StringBuilder(TLUP.StringLength)
        Dim deviceFirmwareRevision As New StringBuilder(TLUP.StringLength)

        Dim resourceCount As UInt32
        Dim isAvailable As Boolean

        '#Region "Search for connected Devices"
        Dim searchObject As New TLUP(IntPtr.Zero) With {
    .ThrowErrorsAsException = throwErrors
    }
        retCode = searchObject.FindResource(resourceCount)

        For devCnt As UInt32 = 0 To resourceCount - 1
            retCode = searchObject.GetResourceInformation(devCnt, deviceModelName, deviceSerialNumber, deviceManufacturer, isAvailable)

            Dim tempConnectedDeviceInfo As New ConnectedDeviceInfo() With {
        .Manufacturer = deviceManufacturer.ToString(),
        .ModelName = deviceModelName.ToString(),
        .SerialNumber = deviceSerialNumber.ToString(),
        .IsAvailable = isAvailable
    }
            connectedDeviceInfo_list.Add(devCnt, tempConnectedDeviceInfo)
        Next
        '#End Region

        '#Region "Select a connected Device"
        Dim chk As Boolean = connectedDeviceInfo_list.TryGetValue(deviceSelection, selectedDeviceInfo)
        If Not chk Then
            ' Handle this case as appropriate.
            Throw New Exception("Invalid device selection.")
        End If
        If Not selectedDeviceInfo.IsAvailable Then
            Dim msg As String = String.Format("Device '{0} - [{1}]' is already used", selectedDeviceInfo.ModelName, selectedDeviceInfo.SerialNumber)
            Throw New Exception(msg)
        End If

        retCode = searchObject.GetResourceName(deviceSelection, deviceResourceName)

        selectedDeviceInfo.ResourceName = deviceResourceName.ToString()

        searchObject.Dispose()
        '#End Region

        '#Region "Open a connected Device"

        upDevice = New TLUP(selectedDeviceInfo.ResourceName, True, True) With {
    .ThrowErrorsAsException = throwErrors
    }

        retCode = upDevice.IdentificationQuery(deviceManufacturer, deviceName, deviceSerialNumber, deviceFirmwareRevision)
        If TLUP_RETURN_CODES.Success <> CType(retCode, TLUP_RETURN_CODES) Then
            Dim errMsg As New StringBuilder(TLUP.StringLength)
            Dim retCodeTemp As Int32 = upDevice.ErrorMessage(retCode, errMsg)
            Throw New Exception(errMsg.ToString())
        End If
        selectedDeviceInfo.Name = deviceName.ToString()
        selectedDeviceInfo.FirmwareRevision = deviceFirmwareRevision.ToString()
        '#End Region

    End Sub
End Class
