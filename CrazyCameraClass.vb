Imports System.Threading
Public Class CrazyCameraClass
    Dim StopCamera As Boolean = True
    Public loopZ, b As Integer

    Public StartEDOF As Boolean = False
    Dim WaiteEDOF As Boolean = False
    Dim AcquireEDOF As Boolean = False
    Dim IdleEDOF As Boolean = False
    Dim LocalTimer As Stopwatch
    Dim StartEDOF_TimeStamp As Integer
    Dim ellapesd(20) As Single
    Public Sub New()
        StopCamera = False
        LocalTimer = New Stopwatch
        loopZ = 0
        b = 0

    End Sub
    Public Sub StartAcqusition()
        StopCamera = False
        loopZ = 0
        b = 0

        Dim StartThread = New Thread(AddressOf StartThreaded)
        StartThread.Start()
    End Sub

    Public Sub StopAcqusition()
        StopCamera = True

    End Sub
    Public Sub StartThreaded()
        IdleEDOF = True
        Do Until StopCamera = True
            If IdleEDOF Then
                Camera.capture(False)
            Else

                Camera.Capture(ScanUnits(b).bytes(ScanUnits(b).loopZ), False)
                'ellapesd(ScanUnits(b).loopZ) = LocalTimer.ElapsedMilliseconds'
                ScanUnits(b).loopZ += 1

            End If

            If StartEDOF Then

                ScanUnits(b).MoveZ()
                LocalTimer.Start()
                StartEDOF = False
                WaiteEDOF = True

                'Do Until LocalTimer.ElapsedMilliseconds > 80

                'Loop


                'LocalTimer.Reset()

            End If

            If WaiteEDOF And LocalTimer.ElapsedMilliseconds > 10 Then
                'ellapesd(20) = LocalTimer.ElapsedMilliseconds
                WaiteEDOF = False
                IdleEDOF = False
                AcquireEDOF = True
            End If

            'If AcquireEDOF = True Then
            '    IdleEDOF = False
            'End If
            If AcquireEDOF = True And ScanUnits(b).loopZ = ScanUnits(b).Z Then
                ScanUnits(b).LoopZdone = True
                ScanUnits(b).loopZ = 0
                AcquireEDOF = False
                IdleEDOF = True
            End If

        Loop

    End Sub

End Class
