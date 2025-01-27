Imports System.IO.Ports
Imports System
Imports System.Threading
Imports System.Xml

Public Class RelayComPort


    Dim Comport As SerialPort

    Shared usbrelay As Byte()() = New Byte()() {New Byte() {&HFF, &H1, &H0}, New Byte() {&HFF, &H1, &H1}, New Byte() {&HFF, &H2, &H0}, New Byte() {&HFF, &H2, &H2}, New Byte() {&HFF, &H3, &H0}, New Byte() {&HFF, &H3, &H3}, New Byte() {&HFF, &H4, &H0}, New Byte() {&HFF, &H4, &H4}, New Byte() {&HFF, &H5, &H0}, New Byte() {&HFF, &H5, &H5}, New Byte() {&HFF, &H6, &H0}, New Byte() {&HFF, &H6, &H6}, New Byte() {&HFF, &H7, &H0}, New Byte() {&HFF, &H7, &H7}, New Byte() {&HFF, &H8, &H0}, New Byte() {&HFF, &H8, &H8}, New Byte() {&H0}, New Byte() {&HFF}}

    Public Sub New(port As String)
        Comport = New SerialPort(port)
        Comport.Open()
    End Sub


    Public Sub LED_ON()
        SetRelays(1, True)
        SetRelays(2, True)
        SetRelays(3, True)
        SetRelays(4, True)
        '     Form1.Button_LED.BackgroundImage = Image.FromFile(Application.StartupPath & "\images\led_diode-on.png")
    End Sub


    Public Sub LED_OFF()
        SetRelays(1, False)
        SetRelays(2, False)
        SetRelays(3, False)
        SetRelays(4, False)
        '   Form1.Button_LED.BackgroundImage = Image.FromFile(Application.StartupPath & "\images\led_diode-off.png")
    End Sub

    Public Sub SetRelays(ByVal RelayN As Integer, State As Boolean)

        'Dim command As Byte()
        'If State Then
        '    command = {CByte(&HFF Xor (1 << (RelayN)))}
        'Else

        '    command = {CByte(&HFF And Not (1 << (RelayN)))}
        'End If
        'Dim command As Byte() = {CByte(&HFF Xor (1 << (RelayN - 1)))}

        Dim RelState As Long
        Dim temp As Long
        If (State = True) Then
            If RelayN = 1 Then temp = 1
            If RelayN = 2 Then temp = 2
            If RelayN = 3 Then temp = 4
            If RelayN = 4 Then temp = 8
            If RelayN = 5 Then temp = 16
            If RelayN = 6 Then temp = 32
            If RelayN = 7 Then temp = 64
            If RelayN = 8 Then temp = 128
            If RelayN = 255 Then temp = 255
            RelState = RelState Or temp
        End If

        If (State = False) Then
            If RelayN = 1 Then temp = 254
            If RelayN = 2 Then temp = 253
            If RelayN = 3 Then temp = 251
            If RelayN = 4 Then temp = 247
            If RelayN = 5 Then temp = 239
            If RelayN = 6 Then temp = 223
            If RelayN = 7 Then temp = 191
            If RelayN = 8 Then temp = 127
            If RelayN = 0 Then temp = 0
            RelState = RelState And temp
        End If



        Dim command As Byte() = {CByte(RelState)}
        Comport.Write(command, 0, command.Length)

    End Sub
End Class
