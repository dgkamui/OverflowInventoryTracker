Imports System.Drawing
Imports System.Windows.Forms
Imports System.Linq

Public Class InteractableMap
    Inherits Control

    ' ===== Public models =====
    Public Class ZoneInfo
        Public Property Name As String
        ' Percentage-based rectangle: X,Y,W,H in 0..1 (relative to control size)
        Public Property BoundsPct As RectangleF
        Public Property IsInventory As Boolean
    End Class

    ' ===== Events =====
    Public Event ZoneClicked(zoneName As String)

    ' ===== State =====
    Private _zones As New List(Of ZoneInfo)
    Private _selected As String = Nothing
    Private _summary As New Dictionary(Of String, List(Of Tuple(Of String, Integer)))(StringComparer.OrdinalIgnoreCase)
    Private _bgImage As Image = Nothing

    ' ===== Appearance =====
    Private ReadOnly colInvFill As Color = Color.FromArgb(232, 243, 255)   ' light blue
    Private ReadOnly colInvBorder As Color = Color.FromArgb(66, 133, 244) ' blue border
    Private ReadOnly colNI_Fill As Color = Color.FromArgb(240, 240, 240)  ' gray fill
    Private ReadOnly colNI_Border As Color = Color.FromArgb(180, 180, 180)
    Private ReadOnly colSelected As Color = Color.FromArgb(90, Color.Gold) ' translucent overlay
    Private ReadOnly colText As Brush = Brushes.Black
    Private ReadOnly tip As New ToolTip With {.AutomaticDelay = 150, .AutoPopDelay = 6000, .ReshowDelay = 100}

    Public Sub New()
        MyBase.New()
        ' High-quality, flicker-free paint
        SetStyle(ControlStyles.AllPaintingInWmPaint Or
                 ControlStyles.UserPaint Or
                 ControlStyles.OptimizedDoubleBuffer Or
                 ControlStyles.ResizeRedraw, True)
        Me.DoubleBuffered = True
        Me.BackColor = Color.White
        Me.Font = New Font("Segoe UI", 9.0F, FontStyle.Regular, GraphicsUnit.Point)
        Me.Cursor = Cursors.Hand
        Me.ResizeRedraw = True
    End Sub

    ' Prevent background erase (we clear in OnPaint)
    Protected Overrides Sub OnPaintBackground(pevent As PaintEventArgs)
        ' Intentionally no base call to avoid flicker during resize.
    End Sub

    ' ===== Painting =====
    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        Dim g = e.Graphics
        g.SmoothingMode = Drawing2D.SmoothingMode.AntiAlias
        g.InterpolationMode = Drawing2D.InterpolationMode.HighQualityBicubic
        g.PixelOffsetMode = Drawing2D.PixelOffsetMode.HighQuality

        ' Clear background
        g.Clear(Me.BackColor)

        ' Optional background image (letterboxed)
        If _bgImage IsNot Nothing Then
            Dim dest = FitRectKeepAspect(_bgImage.Size, Me.ClientRectangle)
            g.DrawImage(_bgImage, dest)
        End If

        ' Draw zones
        For Each z In _zones
            Dim r = ToClientRect(z.BoundsPct)
            If r.Width <= 0 OrElse r.Height <= 0 Then Continue For

            Dim fillCol = If(z.IsInventory, colInvFill, colNI_Fill)
            Dim bordCol = If(z.IsInventory, colInvBorder, colNI_Border)

            Using fill As New SolidBrush(fillCol),
                  pen As New Pen(bordCol, 2.0F)
                g.FillRectangle(fill, r)
                g.DrawRectangle(pen, r)
            End Using

            ' Zone name (top centered, clipped)
            Using fmtName As New StringFormat With {
                .Alignment = StringAlignment.Center,
                .LineAlignment = StringAlignment.Near,
                .FormatFlags = StringFormatFlags.LineLimit,
                .Trimming = StringTrimming.EllipsisCharacter
            }
                Dim nameRect = New RectangleF(r.X, r.Y + 4, r.Width, 18)
                g.DrawString(z.Name, Me.Font, colText, nameRect, fmtName)
            End Using

            ' Inventory summary: draw WRAPPED/ELLIPSIZED inside cell
            If z.IsInventory Then
                Dim items As List(Of Tuple(Of String, Integer)) = Nothing
                If _summary.TryGetValue(z.Name, items) AndAlso items IsNot Nothing AndAlso items.Count > 0 Then
                    Dim lines = items.Select(Function(t) $"{t.Item1}  x{t.Item2}").ToArray()

                    ' Combine lines and let GDI+ wrap/ellipsis within a bounded rectangle
                    Dim textToDraw As String = String.Join(Environment.NewLine, lines) ' many lines; we'll clip/ellipsize
                    Dim textRect As New RectangleF(r.X + 6, r.Y + 24, r.Width - 12, r.Height - 30)

                    Using fmtBody As New StringFormat With {
                        .Alignment = StringAlignment.Near,
                        .LineAlignment = StringAlignment.Near,
                        .FormatFlags = StringFormatFlags.LineLimit,
                        .Trimming = StringTrimming.EllipsisCharacter
                    }
                        g.DrawString(textToDraw, Me.Font, colText, textRect, fmtBody)
                    End Using
                End If
            End If
        Next

        ' Selected overlay drawn last to stay crisp
        If Not String.IsNullOrEmpty(_selected) Then
            Dim zSel = _zones.FirstOrDefault(Function(z) z.Name.Equals(_selected, StringComparison.OrdinalIgnoreCase))
            If zSel IsNot Nothing Then
                Dim r = ToClientRect(zSel.BoundsPct)
                Using br As New SolidBrush(colSelected), pen As New Pen(Color.Gold, 3.0F)
                    g.FillRectangle(br, r)
                    g.DrawRectangle(pen, r)
                End Using
            End If
        End If

        MyBase.OnPaint(e)
    End Sub

    ' ===== API: set data =====
    Public Sub SetZones(zones As List(Of ZoneInfo))
        _zones = If(zones, New List(Of ZoneInfo))
        Invalidate()
    End Sub

    Public Sub SetInventorySummary(summary As Dictionary(Of String, List(Of Tuple(Of String, Integer))))
        If summary Is Nothing Then
            _summary = New Dictionary(Of String, List(Of Tuple(Of String, Integer)))(StringComparer.OrdinalIgnoreCase)
        Else
            _summary = summary
        End If
        ' Update tooltip live if cursor is inside the control
        UpdateToolTipForPoint(PointToClient(MousePosition))
        Invalidate()
    End Sub

    Public Property SelectedZone As String
        Get
            Return _selected
        End Get
        Set(value As String)
            If Not String.Equals(_selected, value, StringComparison.OrdinalIgnoreCase) Then
                _selected = value
                Invalidate()
            End If
        End Set
    End Property

    Public Sub SetBackgroundImage(img As Image)
        _bgImage = img
        Invalidate()
    End Sub

    ' ===== Input / Hit testing =====
    Protected Overrides Sub OnMouseClick(e As MouseEventArgs)
        MyBase.OnMouseClick(e)
        Dim hit = ZoneAtPoint(e.Location)
        If hit IsNot Nothing AndAlso hit.IsInventory Then
            RaiseEvent ZoneClicked(hit.Name)
            SelectedZone = hit.Name
        End If
    End Sub

    Protected Overrides Sub OnMouseMove(e As MouseEventArgs)
        MyBase.OnMouseMove(e)
        UpdateToolTipForPoint(e.Location)
    End Sub

    Private Sub UpdateToolTipForPoint(p As Point)
        Dim hit = ZoneAtPoint(p)
        If hit Is Nothing Then
            tip.Hide(Me)
            Return
        End If

        Dim text As String
        If hit.IsInventory Then
            Dim lines As New List(Of String)
            lines.Add("Location: " & hit.Name)

            Dim items As List(Of Tuple(Of String, Integer)) = Nothing
            If _summary.TryGetValue(hit.Name, items) AndAlso items IsNot Nothing AndAlso items.Count > 0 Then
                For Each t In items.Take(8)
                    lines.Add($"{t.Item1}  x{t.Item2}")
                Next
                If items.Count > 8 Then lines.Add("…")
            Else
                lines.Add("(empty)")
            End If

            text = String.Join(Environment.NewLine, lines)
        Else
            text = "Non-inventory area"
        End If

        ' Show tooltip near cursor
        tip.Show(text, Me, p.X + 16, p.Y + 16, 2000)
    End Sub

    ' ===== Helpers =====
    Private Function ZoneAtPoint(p As Point) As ZoneInfo
        For Each z In _zones
            Dim r = ToClientRect(z.BoundsPct)
            If r.Contains(p) Then Return z
        Next
        Return Nothing
    End Function

    Private Function ToClientRect(pct As RectangleF) As Rectangle
        Dim w As Integer = Math.Max(0, CInt(Math.Round(pct.Width * Me.ClientSize.Width)))
        Dim h As Integer = Math.Max(0, CInt(Math.Round(pct.Height * Me.ClientSize.Height)))
        Dim x As Integer = CInt(Math.Round(pct.X * Me.ClientSize.Width))
        Dim y As Integer = CInt(Math.Round(pct.Y * Me.ClientSize.Height))
        Return New Rectangle(x, y, w, h)
    End Function

    Private Function FitRectKeepAspect(imgSize As Size, dest As Rectangle) As Rectangle
        If imgSize.Width = 0 OrElse imgSize.Height = 0 OrElse dest.Width = 0 OrElse dest.Height = 0 Then
            Return dest
        End If
        Dim s As Single = Math.Min(dest.Width / imgSize.Width, dest.Height / imgSize.Height)
        Dim w As Integer = CInt(imgSize.Width * s)
        Dim h As Integer = CInt(imgSize.Height * s)
        Dim x As Integer = dest.X + (dest.Width - w) \ 2
        Dim y As Integer = dest.Y + (dest.Height - h) \ 2
        Return New Rectangle(x, y, w, h)
    End Function

End Class
