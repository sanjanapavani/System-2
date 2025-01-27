Public Class Relay

    ' Declare functions from the FTD2XX.dll library

    Private Declare Function FT_Open Lib "FTD2XX.dll" (ByVal intDeviceNumber As Integer, ByRef lnghandle As Long) As Long

    Private Declare Function FT_OpenEx Lib "FTD2XX.dll" (ByVal arg1 As String, ByVal arg2 As Long, ByRef lnghandle As Long) As Long

    Private Declare Function FT_Close Lib "FTD2XX.dll" (ByVal lnghandle As Long) As Long

    Private Declare Function FT_Read Lib "FTD2XX.dll" (ByVal lnghandle As Long, ByVal lpszBuffer As String, ByVal lngBufferSize As Long, ByRef lngBytesReturned As Long) As Long

    Private Declare Function FT_Write Lib "FTD2XX.dll" (ByVal lnghandle As Long, ByVal lpszBuffer As String, ByVal lngBufferSize As Long, ByRef lngBytesWritten As Long) As Long

    Private Declare Function FT_SetBaudRate Lib "FTD2XX.dll" (ByVal lnghandle As Long, ByVal lngBaudRate As Long) As Long

    Private Declare Function FT_SetDataCharacteristics Lib "FTD2XX.dll" (ByVal lnghandle As Long, ByVal byWordLength As Byte, ByVal byStopBits As Byte, ByVal byParity As Byte) As Long

    Private Declare Function FT_SetFlowControl Lib "FTD2XX.dll" (ByVal lnghandle As Long, ByVal intFlowControl As Integer, ByVal byXonChar As Byte, ByVal byXoffChar As Byte) As Long

    Private Declare Function FT_ResetDevice Lib "FTD2XX.dll" (ByVal lnghandle As Long) As Long

    Private Declare Function FT_SetDtr Lib "FTD2XX.dll" (ByVal lnghandle As Long) As Long

    Private Declare Function FT_ClrDtr Lib "FTD2XX.dll" (ByVal lnghandle As Long) As Long

    Private Declare Function FT_SetRts Lib "FTD2XX.dll" (ByVal lnghandle As Long) As Long

    Private Declare Function FT_ClrRts Lib "FTD2XX.dll" (ByVal lnghandle As Long) As Long

    Private Declare Function FT_GetModemStatus Lib "FTD2XX.dll" (ByVal lnghandle As Long, ByRef lngModemStatus As Long) As Long

    Private Declare Function FT_Purge Lib "FTD2XX.dll" (ByVal lnghandle As Long, ByVal lngMask As Long) As Long

    Private Declare Function FT_GetStatus Lib "FTD2XX.dll" (ByVal lnghandle As Long, ByRef lngRxBytes As Long, ByRef lngTxBytes As Long, ByRef lngEventsDWord As Long) As Long

    Private Declare Function FT_GetQueueStatus Lib "FTD2XX.dll" (ByVal lnghandle As Long, ByRef lngRxBytes As Long) As Long

    Private Declare Function FT_GetEventStatus Lib "FTD2XX.dll" (ByVal lnghandle As Long, ByRef lngEventsDWord As Long) As Long

    Private Declare Function FT_SetChars Lib "FTD2XX.dll" (ByVal lnghandle As Long, ByVal byEventChar As Byte, ByVal byEventCharEnabled As Byte, ByVal byErrorChar As Byte, ByVal byErrorCharEnabled As Byte) As Long

    Private Declare Function FT_SetTimeouts Lib "FTD2XX.dll" (ByVal lnghandle As Long, ByVal lngReadTimeout As Long, ByVal lngWriteTimeout As Long) As Long

    Private Declare Function FT_SetBreakOn Lib "FTD2XX.dll" (ByVal lnghandle As Long) As Long

    Private Declare Function FT_SetBreakOff Lib "FTD2XX.dll" (ByVal lnghandle As Long) As Long

    Private Declare Function FT_ListDevices Lib "FTD2XX.dll" (ByVal arg1 As Long, ByVal arg2 As String, ByVal dwFlags As Long) As Long

    Private Declare Function FT_GetNumDevices Lib "FTD2XX.dll" Alias "FT_ListDevices" (ByRef arg1 As Long, ByVal arg2 As String, ByVal dwFlags As Long) As Long

    Private Declare Function FT_SetBitMode Lib "FTD2XX.dll" (ByVal lnghandle As Long, ByVal mask As Byte, ByVal enable As Byte) As Long

    ' Return codes

    Const FT_OK = 0

    Const FT_INVALID_HANDLE = 1

    Const FT_DEVICE_NOT_FOUND = 2

    Const FT_DEVICE_NOT_OPENED = 3

    Const FT_IO_ERROR = 4

    Const FT_INSUFFICIENT_RESOURCES = 5

    Const FT_INVALID_PARAMETER = 6

    Const FT_INVALID_BAUD_RATE = 7

    ' Word Lengths

    Const FT_BITS_8 = 8

    Const FT_BITS_7 = 7

    ' Stop Bits

    Const FT_STOP_BITS_1 = 0

    Const FT_STOP_BITS_1_5 = 1

    Const FT_STOP_BITS_2 = 2

    ' Parity

    Const FT_PARITY_NONE = 0

    Const FT_PARITY_ODD = 1

    Const FT_PARITY_EVEN = 2

    Const FT_PARITY_MARK = 3

    Const FT_PARITY_SPACE = 4

    ' Flow Control

    Const FT_FLOW_NONE = &H0

    Const FT_FLOW_RTS_CTS = &H100

    Const FT_FLOW_DTR_DSR = &H200

    Const FT_FLOW_XON_XOFF = &H400

    ' Purge rx and tx buffers

    Const FT_PURGE_RX = 1

    Const FT_PURGE_TX = 2

    ' Flags for FT_OpenEx

    Const FT_OPEN_BY_SERIAL_NUMBER = 1

    Const FT_OPEN_BY_DESCRIPTION = 2

    ' Flags for FT_ListDevices

    Const FT_LIST_BY_NUMBER_ONLY = &H80000000

    Const FT_LIST_BY_INDEX = &H40000000

    Const FT_LIST_ALL = &H20000000

    Dim RelState As Long

    Public IsLED_ON As Boolean

    Public status As Boolean

    Dim lnghandle As Long

    Dim ThorlabsLED As LEDStructure

    Const BlueLED_RichardMode As Integer = 4

    Const BlueLED As Integer = 3

    Const BacklightWhiteLED As Integer = 2

    'Const GreenLED As Integer = 3

    Public Sub New(port As Integer)

        status = False

        Try

            RelState = 0

            Dim arg1 As Long

            Dim arg2 As String = ""

            Dim dwFlag As Long

            Dim NumDevices As Long = FT_GetNumDevices(arg1, arg2, dwFlag)

            Dim List As Long = FT_ListDevices(arg1, arg2, dwFlag)

            If FT_Open(port, lnghandle) <> FT_OK Then

                MsgBox("Error while opening")

                Exit Sub

            End If

            If FT_SetBitMode(lnghandle, 255, 4) <> FT_OK Then

                MsgBox("Error setting bit mode")

                Exit Sub

            End If

            If FT_SetBaudRate(lnghandle, 921600) <> FT_OK Then

                MsgBox("Error setting baud rate")

                Exit Sub

            Else

                status = True

            End If

        Catch ex As Exception

            MsgBox("Exception: " & ex.Message)

        End Try

        ThorlabsLED = New LEDStructure

        ThorlabsLED.SetLEDCurrent(0.5)

    End Sub

    Public Sub SetRelays(ByVal RelayN As Integer, State As Boolean)

        If status = False Then Exit Sub

        Dim temp As Long = If(State, (1 << (RelayN - 1)), Not (1 << (RelayN - 1)) And &HFF)

        RelState = If(State, RelState Or temp, RelState And temp)

        Dim strWriteBuffer As String = Chr(RelState)

        Dim lngBytesWritten As Integer = 0

        If FT_Write(lnghandle, strWriteBuffer, Len(strWriteBuffer), lngBytesWritten) <> FT_OK Then
            MsgBox("Error writing to device")
            Exit Sub
        End If

        If State Then
            Select Case RelayN
                Case BlueLED_RichardMode
                    ThorlabsLED.SetLEDCurrent(0.1)
                    ThorlabsLED.TurnOn()
                Case BlueLED
                    ThorlabsLED.SetLEDCurrent(0.9)
                    ThorlabsLED.TurnOn()
            End Select
        Else
            ThorlabsLED.TurnOff()
        End If

    End Sub

    Public Sub relay_Terminate()

        FT_Close(lnghandle)

    End Sub

    Public Sub LED_ON()

        For i As Integer = 1 To 4

            SetRelays(i, True)

        Next

    End Sub

    Public Sub LED_OFF()

        For i As Integer = 1 To 4

            SetRelays(i, False)

        Next

    End Sub

End Class