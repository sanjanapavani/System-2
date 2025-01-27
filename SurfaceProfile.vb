Imports AForge.Imaging
Imports AForge.Imaging.Filters
Imports System.Runtime.Serialization.Formatters.Binary

Public Class SurfaceProfile
    Dim z_platform As Integer()
    Dim z_index_d As Double()
    Dim ZMap As Single(,)
    Dim edge_intensity As Double()
    Dim n_stages As Integer '= 379
    Dim z_step_size As Integer '= 50 '10 'in microns
    Public rr(), gg(), bb() As Byte
    Dim plot_needed As Boolean
    Dim BLure As FFTW_VB_Real
    Dim Fftw_vb As FFTW_VB
    Dim is_processed As Boolean
    Dim localFOVsz As Integer
    Dim GlobalX As Integer '= 2048 '2098
    Dim GlobalY As Integer '= 1576 '1640 '1642
    Dim stage_begin As Integer
    Dim stages_used As Integer
    Dim GreySingle(n_stages)() As Single
    Dim tempX As Integer '= GlobalX * 2 / 9
    Dim midWindowX As Integer '= tempX * 4 'GlobalX / 2
    Dim tempY As Integer '= GlobalY * 2 / 9
    Dim midWindowY As Integer '= tempY * 4 'GlobalY / 2
    Dim n_local_x As Integer '= midWindowX * 0.5 '204 'midWindowX '400 '100
    Dim n_local_y As Integer '= midWindowY * 0.5 '164 'midWindowY '320 '80
    Dim is_stage_range_changed As Boolean
    Dim is_prefix_sum_made As Boolean
    Dim mid_img, mid_edg_img As Bitmap
    Dim Heatmap As FastBMP
    Dim trim_start As Integer '= 15
    Dim trim_total As Integer '= 30
    Dim min_intensity As Double
    Dim q_length As Integer '= 20
    Dim focal_length_X As Double
    Dim focal_length_Y As Double
    Dim z_plate_offset_X As Double
    Dim z_plate_offset_Y As Double
    Dim x_plate_offset As Double
    Dim y_plate_offset As Double
    Dim plate_W As Double '= 30000 '50000 'Need to change it to the actual width of the plate
    Dim plate_H As Double '= 23086
    Dim path_raw_img As String '= "C:\Users\Histo\OneDrive\Profile estimator\EDOF 50um 380 steps\"
    Dim path_rect As String '= "c:\temp\ProfileEstimator\BinarySerialization_rect.dat"

    Public Sub New(n_stage As Integer, z_step As Integer, stage_start As Integer)
        n_stages = 379
        z_step_size = z_step
        GlobalX = 2048
        GlobalY = 1576
        stages_used = n_stage
        stage_begin = stage_start
        tempX = GlobalX * 2 / 9
        midWindowX = tempX * 4
        tempY = GlobalY * 2 / 9
        midWindowY = tempY * 4
        n_local_x = midWindowX * 0.5
        n_local_y = midWindowY * 0.5
        trim_start = 15
        trim_total = 30
        q_length = 20
        plate_W = 30000
        plate_H = 23086
        path_raw_img = "C:\Users\Histo\OneDrive\Profile estimator\EDOF 50um 380 steps\"
        path_rect = "c:\temp\ProfileEstimator\BinarySerialization_rect.dat"
    End Sub

    Private Function SobelEdge_single(ByRef grey As FastBMP) As Single()
        'takes a FastBMP and makes a Single array of edge values:
        '
        'P1 P2 P3
        'P8  x P4
        'P7 P6 P5
        '
        '|G| = |P1 + 2P2 + P3 - P7 - 2P6 - P5| + |P3 + 2P4 + P5 - P1 - 2P8 - P7|
        'sum of squares is used insted
        Dim edge As Single() = New Single(midWindowY * midWindowX - 1) {}
        'grey_mid is made by padding the middle area by 1 px on all 4 sides
        midWindowX += 2
        midWindowY += 2
        Dim grey_mid As Single() = New Single(midWindowY * midWindowX - 1) {}

        Dim outX As Integer = (grey.width - midWindowX)
        Dim i_b As Integer = (grey.height - midWindowY) * 0.5
        i_b *= grey.stride
        i_b += outX * 0.5
        Dim i_g As Integer = 0

        For j = 0 To midWindowY - 1
            For i = 0 To midWindowX - 1
                grey_mid(i_g) = grey.bytes(i_b)
                i_b += 1
                i_g += 1
            Next
            i_b += grey.offset + outX
        Next

        Dim Gx, Gy As Single
        For j = 1 To midWindowY - 2
            For i = 1 To midWindowX - 2
                Dim i_center As Integer = j * midWindowX + i
                Dim i_midup As Integer = i_center - midWindowX
                Dim i_middown As Integer = i_center + midWindowX
                Gx = grey_mid(i_midup + 1) - grey_mid(i_middown - 1)
                Gy = Gx
                Dim diag As Single = grey_mid(i_middown + 1) - grey_mid(i_midup - 1)
                Gx += diag
                Gy -= diag
                Gx += 2 * grey_mid(i_center + 1)
                Gx -= 2 * grey_mid(i_center - 1)
                Gy += 2 * grey_mid(i_midup)
                Gy -= 2 * grey_mid(i_middown)
                Dim i_edg As Integer = (j - 1) * (midWindowX - 2) + i - 1
                edge(i_edg) = Gx * Gx + Gy * Gy 'Math.Sqrt(Gx * Gx + Gy * Gy)
                If grey_mid(i_center) > 0 Then
                    edge(i_edg) *= 256 / grey_mid(i_center)
                Else
                    edge(i_edg) = 65280
                End If
            Next
        Next
        midWindowX -= 2
        midWindowY -= 2
        Return edge
    End Function

    Private Sub ProcessImages(blurQc As Single, pbar As ProgressBar)
        is_processed = False
        is_prefix_sum_made = False

        Dim filter_gray As New Grayscale(0.2125, 0.7154, 0.0721)

        Dim threshold = 70 '50 '42
        Dim blob_ctr As New BlobCounter With {
            .FilterBlobs = True,
            .MinWidth = 50,
            .MinHeight = 50,
            .ObjectsOrder = ObjectsOrder.Size,
            .BackgroundThreshold = Color.FromArgb(threshold, threshold, threshold)
        }

        Dim Edge_homogenity As New HomogenityEdgeDetector
        Dim Edge_sobel As New SobelEdgeDetector
        Dim ContrastStretch_filter As New ContrastStretch()
        Dim ResizeBilinear_filter As New ResizeBilinear(GlobalX, GlobalY)

        BLure = New FFTW_VB_Real(midWindowY, midWindowX)
        BLure.MakeGaussianReal(blurQc, BLure.MTF, 2) '0.008 too much, 0.05 not much

        Dim list_rect As New List(Of Rectangle)
        Using stream_rect As New IO.FileStream(path_rect, IO.FileMode.Open)
            Dim formatter As New BinaryFormatter
            list_rect = formatter.Deserialize(stream_rect)
            stream_rect.Close()
        End Using

        pbar.Visible = True
        pbar.Maximum = n_stages
        For i_stage As Integer = 0 To n_stages
            Dim img_raw As New Bitmap(path_raw_img & i_stage.ToString("D4") & ".bmp")

            Dim grayImage As Bitmap = filter_gray.Apply(img_raw)
            Dim cropfilter As IFilter

            cropfilter = New Crop(list_rect(i_stage))

            Dim resized_img As Bitmap = ResizeBilinear_filter.Apply(cropfilter.Apply(grayImage))

            'If CheckBox_SaveCropped.Checked Then
            '    resized_img.Save("Cropped_images\plate_" & i_stage.ToString("D4") & ".jpg")
            'End If

            Dim mid_stage As Integer = stage_begin + 0.5 * stages_used
            If i_stage = mid_stage Then
                Dim crop_mid As IFilter
                Dim rect_mid As New Rectangle((GlobalX - midWindowX) / 2, (GlobalY - midWindowY) / 2, midWindowX, midWindowY)
                crop_mid = New Crop(rect_mid)
                mid_img = crop_mid.Apply(resized_img)
                'Dim mid_img As Bitmap = crop_mid.Apply(bmp_img)
                'SaveSinglePage32("c:\temp\ProfileEstimator\midedge.tif", GreySingle(i_stage), midWindowX, midWindowY)
                'Dim edg_display As New Bitmap("c:\temp\ProfileEstimator\midedge.tif")
                'PictureBox1.Image = mid_img
            End If

            Dim fastImg As New FastBMP(resized_img)
            GreySingle(i_stage) = SobelEdge_single(fastImg)

            If i_stage = mid_stage Then
                SaveSinglePage32("c:\temp\ProfileEstimator\midedge.tif", GreySingle(i_stage), midWindowX, midWindowY)
                mid_edg_img = New Bitmap("c:\temp\ProfileEstimator\midedge.tif")
            End If

            'If CheckBox_FFTW_blur.Checked Then
            '    BLure.UpLoad(GreySingle(i_stage))
            '    BLure.Process_FT_MTF()
            '    BLure.DownLoad(GreySingle(i_stage))
            'End If

            'If Me.CheckBox_SaveEdge.Checked Then
            '    SaveSinglePage32("c:\temp\ProfileEstimator\" + i_stage.ToString("D4") + ".tif", GreySingle(i_stage), midWindowX, midWindowY)
            'End If
            pbar.Value = i_stage
        Next

        is_stage_range_changed = True
        pbar.Visible = False
        is_processed = True
    End Sub

    Private Function Poly_fit(ByRef ydata() As Double) As Single
        Dim q_trim As Queue(Of Double) = New Queue(Of Double)()
        Dim q_total(stages_used - q_length) As Double
        q_total(0) = 0
        For i_z = 0 To q_length - 1
            q_trim.Enqueue(ydata(i_z))
            q_total(0) += ydata(i_z)
        Next
        Dim q_total_l As Double = q_total(0)
        For i_z = q_length To stages_used - 1
            q_trim.Enqueue(ydata(i_z))
            q_total(i_z - q_length + 1) = q_total(i_z - q_length) + ydata(i_z) - q_trim.Dequeue
        Next

        Dim max_index As Integer = 0
        For i_z = 0 To stages_used - q_length
            If q_total(i_z) > q_total(max_index) Then max_index = i_z
        Next

        Dim q_total_r As Double = q_total.Last
        Dim q_total_max, MovAvg_cutoff As Double
        q_total_max = q_total(max_index)
        Dim z_start_centroid, z_end_centroid As Integer
        If q_total_l > q_total_r Then
            MovAvg_cutoff = 0.4 * q_total_l + 0.6 * q_total_max
            'MovAvg_cutoff = 0.5 * (q_total_l + q_total_max)
        Else
            MovAvg_cutoff = 0.4 * q_total_r + 0.6 * q_total_max
            'MovAvg_cutoff = 0.5 * (q_total_r + q_total_max)
        End If
        For i_z = 0 To stages_used - q_length
            If q_total(i_z) >= MovAvg_cutoff Then
                z_start_centroid = i_z '- 0.5 * q_length
                Exit For
            End If
        Next
        For i_z = 0 To stages_used - q_length
            If q_total(stages_used - q_length - i_z) >= MovAvg_cutoff Then
                z_end_centroid = stages_used - i_z - q_length
                Exit For
            End If
        Next

        Dim centroid As Double = 0
        Dim area As Double = 0
        For i As Integer = z_start_centroid To z_end_centroid
            Dim del As Double = (ydata(i) - MovAvg_cutoff) ^ 6
            centroid += del * z_index_d(i + 0.5 * q_length)
            area += del
        Next
        centroid /= area
        centroid += stage_begin
        centroid *= z_step_size
        Return centroid
    End Function

    Private Function Estimate_z_global(pt_global As Point) As Single
        ReDim edge_intensity(stages_used)
        Dim min_index As Integer = 0
        For i_z As Integer = 0 To stages_used 'n_stages
            Dim pt_orig_begin As Point = pt_global
            If pt_orig_begin.X - localFOVsz / 2 < 0 Then
                pt_orig_begin.X = 0
            Else pt_orig_begin.X = pt_orig_begin.X - localFOVsz / 2
            End If
            If pt_orig_begin.Y - localFOVsz / 2 < 0 Then
                pt_orig_begin.Y = 0
            Else pt_orig_begin.Y = pt_orig_begin.Y - localFOVsz / 2
            End If

            Dim pt_orig_end As Point = pt_global
            If pt_orig_end.X + localFOVsz / 2 > midWindowX - 1 Then
                pt_orig_end.X = midWindowX - 1
            Else pt_orig_end.X = pt_orig_end.X + localFOVsz / 2
            End If
            If pt_orig_end.Y + localFOVsz / 2 > midWindowY - 1 Then
                pt_orig_end.Y = midWindowY - 1
            Else pt_orig_end.Y = pt_orig_end.Y + localFOVsz / 2
            End If

            'pt_orig_end.X -= 1
            'pt_orig_end.Y -= 1

            If is_prefix_sum_made Then
                'prefix sum matrix is computed and saved in GreySingle
                edge_intensity(i_z) = GreySingle(i_z + stage_begin)(pt_orig_end.Y * midWindowX + pt_orig_end.X)
                If pt_orig_begin.Y Then
                    edge_intensity(i_z) -= GreySingle(i_z + stage_begin)((pt_orig_begin.Y - 1) * midWindowX + pt_orig_end.X)
                    If pt_orig_begin.X Then
                        edge_intensity(i_z) -= GreySingle(i_z + stage_begin)(pt_orig_end.Y * midWindowX + pt_orig_begin.X - 1)
                        edge_intensity(i_z) += GreySingle(i_z + stage_begin)((pt_orig_begin.Y - 1) * midWindowX + pt_orig_begin.X - 1)
                    End If
                Else
                    If pt_orig_begin.X Then
                        edge_intensity(i_z) -= GreySingle(i_z + stage_begin)(pt_orig_end.Y * midWindowX + pt_orig_begin.X - 1)
                    End If
                End If

            Else
                'GreySingle contains edge values, prefix sum has not been computed
                edge_intensity(i_z) = 0

                For j As Integer = pt_orig_begin.Y To pt_orig_end.Y
                    For i As Integer = pt_orig_begin.X To pt_orig_end.X
                        edge_intensity(i_z) += GreySingle(i_z + stage_begin)(j * midWindowX + i)
                        'edge_intensity(i_z) += platformz_edge(i_z).GetPixel(i, j).G
                    Next
                Next
            End If

            Dim n_px As Integer = (pt_orig_end.Y - pt_orig_begin.Y + 1) * (pt_orig_end.X - pt_orig_begin.X + 1)
            edge_intensity(i_z) /= n_px
            If edge_intensity(i_z) < edge_intensity(min_index) Then
                min_index = i_z
            End If
            'find the index of maximum edge_intensity(index)
            'If (edge_intensity(i_z) >= edge_intensity(focus_index)) Then focus_index = i_z
        Next
        min_intensity = edge_intensity(min_index) 'Check this: is it needed anylonger?

        'If mtd_curvefit Then
        '    'transforme edge_intensity by the natural log to fit a Gaussian distribution
        '    For i_z As Integer = 0 To stages_used
        '        edge_intensity(i_z) += 2 - min_intensity
        '        'edge_intensity(i_z) *= edge_intensity(i_z)
        '        edge_intensity(i_z) = Math.Log(edge_intensity(i_z))
        '    Next
        'End If
        Return Poly_fit(edge_intensity)
    End Function

    Public Sub Load_colormap()
        ReDim rr(1000), gg(1000), bb(1000)

        FileOpen(1, "rgb.txt", OpenMode.Input)

        Dim t As Integer
        Do Until EOF(1)
            t += 1
            Input(1, rr(t))
            Input(1, gg(t))
            Input(1, bb(t))
        Loop
        FileClose(1)
    End Sub

    Private Sub HeatMapped(ByRef zMap(,) As Single, fileName As String, heatmap As FastBMP)
        Dim DimX As Integer
        DimX = zMap.GetLength(1)

        Dim min, max As Double
        max = Double.MinValue '0.001
        min = Double.MaxValue '0
        For j = 0 To zMap.GetLength(0) - 1
            For i = 0 To DimX - 1
                If zMap(j, i) > max Then max = zMap(j, i)
                If zMap(j, i) < min Then min = zMap(j, i)
            Next
        Next

        Load_colormap()
        Dim index As Integer
        For j = 0 To zMap.GetLength(0) - 1
            For i = 0 To DimX - 1
                index = (zMap(j, i) - min) / (max - min) * 1000
                heatmap.FillOriginalPixel(i, j, rr(index), gg(index), bb(index))
            Next
        Next
        heatmap.Reset()
        'Heatmap.bmp.Save(fileName & ".jpg")
    End Sub

    Private Sub Make_PrefixSum2d()
        For i_z = 0 To n_stages 'stage_begin
            For j = 0 To midWindowY - 1
                For i = 1 To midWindowX - 1
                    Dim ind_ij As Integer = j * midWindowX + i
                    GreySingle(i_z)(ind_ij) += GreySingle(i_z)(ind_ij - 1)
                Next
            Next
            For i = 0 To midWindowX - 1
                For j = 1 To midWindowY - 1
                    Dim ind_ij As Integer = j * midWindowX + i
                    GreySingle(i_z)(ind_ij) += GreySingle(i_z)(ind_ij - midWindowX)
                Next
            Next
        Next
    End Sub

    Private Sub z_map(blurQc As Single, pbar As ProgressBar)
        Dim pt_global As Point
        ReDim ZMap(n_local_y - 1, n_local_x - 1)
        plot_needed = False
        Dim GreyZmap = New Single(n_local_y * n_local_x - 1) {}
        'Fftw_vb = New FFTW_VB(n_stages, 1)
        pbar.Visible = True
        pbar.Maximum = n_local_y * n_local_x

        'Prefix sum is computed and stored in GreySingle() for Efficient Algorighm:
        If Not is_prefix_sum_made Then
            Make_PrefixSum2d()
            is_prefix_sum_made = True
        End If

        Dim Xres As Single = midWindowX / n_local_x
        Dim Yres As Single = midWindowY / n_local_y
        For j As Integer = 0 To n_local_y - 1
            For i As Integer = 0 To n_local_x - 1
                'pt_local.X = i
                'pt_local.Y = j
                'Dim focus_ind As Integer = Estimate_z_local(pt_local)
                pt_global.X = i * Xres
                pt_global.Y = j * Yres
                Dim focus_z As Integer = Estimate_z_global(pt_global)
                ZMap(j, i) = focus_z 'z_platform(focus_ind)
                GreyZmap(n_local_x * j + i) = focus_z 'z_platform(focus_ind)
                pbar.Value = j * n_local_x + i + 1
            Next
        Next

        'Dim Heatmap As New FastBMP(n_local_x, n_local_y, Imaging.PixelFormat.Format24bppRgb)
        Heatmap = New FastBMP(n_local_x, n_local_y, Imaging.PixelFormat.Format24bppRgb)
        HeatMapped(ZMap, "zMap", Heatmap)

        Dim Smooth As AForge.Imaging.Filters.BilateralSmoothing
        Smooth = New Filters.BilateralSmoothing
        Smooth.KernelSize = 7
        Smooth.SpatialFactor = 10 '10
        Smooth.ColorFactor = 60
        Smooth.ColorPower = 0.5
        Smooth.ApplyInPlace(Heatmap.bmp)
        'PictureBox1.Image = Heatmap.bmp 'check whether it is needed?
        'TabControl1.SelectedTab = TabPage3
        Heatmap.bmp.Save("ZMap" & ".jpg")

        BLure = New FFTW_VB_Real(n_local_y, n_local_x)
        BLure.MakeGaussianReal(blurQc, BLure.MTF, 2) '0.008 too much, 0.05 not much
        SaveSinglePage32("zMapGrey" + stages_used + "stages_from" + stage_begin + ".tif", GreyZmap, n_local_x, n_local_y)
        BLure.UpLoad(GreyZmap)
        BLure.Process_FT_MTF()
        BLure.DownLoad(GreyZmap)
        SaveSinglePage32("zMapGrey_smoothed" + stages_used + "stages_from" + stage_begin + ".tif", GreyZmap, n_local_x, n_local_y)
        pbar.Visible = False
    End Sub

    Private Sub CalibrateEmptyPlate(pbar As ProgressBar, chart As DataVisualization.Charting.Chart)
        pbar.Visible = True
        pbar.Maximum = n_stages
        Dim list_rect As New List(Of Rectangle)
        'If RadioButton_Calibrate.Checked Then
        Dim filter_gray As New Grayscale(0.2125, 0.7154, 0.0721)
        Dim threshold = 56 '50 '42 'lets try higher threshold to reduce the flicker when cropping out of focus images
        Dim blob_ctr As New BlobCounter With {
            .FilterBlobs = True,
            .MinWidth = 50,
            .MinHeight = 50,
            .ObjectsOrder = ObjectsOrder.Size,
            .BackgroundThreshold = Color.FromArgb(threshold, threshold, threshold)
        }
        'Dim cropfilter As IFilter

        For i_stage As Integer = 0 To n_stages
            Dim img_raw As New Bitmap(path_raw_img & i_stage.ToString("D4") & ".bmp")
            Dim grayImage As Bitmap = filter_gray.Apply(img_raw)
            blob_ctr.ProcessImage(grayImage)
            Dim rects As Rectangle() = blob_ctr.GetObjectsRectangles()
            list_rect.Add(rects(0))
            pbar.Value = i_stage
        Next
        '    Using stream_rect As New IO.FileStream(path_rect, IO.FileMode.Create)
        '        Dim formatter As New BinaryFormatter
        '        formatter.Serialize(stream_rect, list_rect)
        '        stream_rect.Close()
        '    End Using
        'Else
        '    Using stream_rect As New IO.FileStream(path_rect, IO.FileMode.Open)
        '        Dim formatter As New BinaryFormatter
        '        list_rect = formatter.Deserialize(stream_rect)
        '        stream_rect.Close()
        '    End Using
        'End If

        chart.Series(0).Points.Clear()
        chart.Series(1).Points.Clear()
        'Me.Chart1.Series(2).Points.Clear()
        chart.Series(0).Color = Color.Blue
        chart.Series(1).Color = Color.Red
        'Me.Chart1.Series(2).Color = Color.Green
        Dim z_y(n_stages) As Double
        Dim inverse_w(n_stages) As Double
        Dim inverse_h(n_stages) As Double
        'Dim inverse_combined_hw(n_stages) As Double
        For i_stage As Integer = 0 To n_stages
            z_y(i_stage) = i_stage * z_step_size
            inverse_w(i_stage) = plate_W / list_rect(i_stage).Width
            inverse_h(i_stage) = plate_H / list_rect(i_stage).Height
            'inverse_combined_hw(i_stage) = 0.5 * (inverse_w(i_stage) + inverse_h(i_stage))
            'Me.Chart1.Series(1).Points.AddXY(inverse_w(i_stage), z_y(i_stage))
            'Me.Chart1.Series(0).Points.AddXY(inverse_h(i_stage), z_y(i_stage))
        Next
        'Dim lineFit_combined_hw = MathNet.Numerics.Fit.Line(inverse_combined_hw, z_y)
        'z_plate_offset = lineFit_combined_hw.A
        'focal_length = -lineFit_combined_hw.B
        Dim lineFit_w = MathNet.Numerics.Fit.Line(inverse_w, z_y)
        z_plate_offset_X = lineFit_w.A
        focal_length_X = -lineFit_w.B '/ plate_W
        Dim lineFit_h = MathNet.Numerics.Fit.Line(inverse_h, z_y)
        z_plate_offset_Y = lineFit_h.A
        focal_length_Y = -lineFit_h.B '/ plate_H

        Dim x_left(n_stages) As Double
        Dim y_top(n_stages) As Double
        Dim FW_z(n_stages) As Double
        Dim FH_z(n_stages) As Double

        For i_stage As Integer = 0 To n_stages '* 0.9
            FW_z(i_stage) = focal_length_X / (z_plate_offset_X - z_y(i_stage))
            FH_z(i_stage) = focal_length_Y / (z_plate_offset_Y - z_y(i_stage))
            x_left(i_stage) = list_rect(i_stage).X
            y_top(i_stage) = list_rect(i_stage).Y
            'Me.Chart1.Series(0).Points.AddXY(FW_z(i_stage), x_left(i_stage))
            'Me.Chart1.Series(1).Points.AddXY(FH_z(i_stage), y_top(i_stage))
        Next
        'Me.Chart1.ChartAreas(0).RecalculateAxesScale()
        'Me.Chart1.ChartAreas(0).AxisY.IsStartedFromZero = False
        Dim lineFit_x_left = MathNet.Numerics.Fit.Line(FW_z, x_left)
        Dim lineFit_y_top = MathNet.Numerics.Fit.Line(FH_z, y_top)
        Dim x0 As Double = lineFit_x_left.A
        Dim w_left As Double = -lineFit_x_left.B
        Dim y0 As Double = lineFit_y_top.A
        Dim h_top As Double = -lineFit_y_top.B
        Dim rect_smooth As Rectangle
        For i_stage As Integer = 0 To n_stages
            Dim x_smooth As Double = x0 - w_left * FW_z(i_stage)
            Dim y_smooth As Double = y0 - h_top * FH_z(i_stage)
            Dim w_smooth As Double = plate_W * FW_z(i_stage)
            Dim h_smooth As Double = plate_H * FH_z(i_stage)
            rect_smooth = New Rectangle(x_smooth, y_smooth, w_smooth, h_smooth)
            list_rect(i_stage) = rect_smooth
        Next
        Using stream_rect As New IO.FileStream(path_rect, IO.FileMode.Create)
            Dim formatter As New BinaryFormatter
            formatter.Serialize(stream_rect, list_rect)
            stream_rect.Close()
        End Using
        pbar.Visible = False
    End Sub
End Class
