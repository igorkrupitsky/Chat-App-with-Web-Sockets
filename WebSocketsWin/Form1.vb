Imports System.ComponentModel
Imports System.Net.WebSockets

Public Class Form1
    Dim oSocket As System.Net.WebSockets.ClientWebSocket = Nothing

    Private Async Sub btnOpen_Click(sender As Object, e As EventArgs) Handles btnOpen.Click

        System.Net.ServicePointManager.SecurityProtocol =
            System.Net.SecurityProtocolType.Ssl3 Or
            System.Net.SecurityProtocolType.Tls12 Or
            System.Net.SecurityProtocolType.Tls11 Or
            System.Net.SecurityProtocolType.Tls

        Dim sServer As String = txtURL.Text 'localhost/WebSocket
        If sServer = "" Then
            MsgBox("Server URL is missing")
            Exit Sub
        End If

        Dim sProtocol As String = "ws"
        If chkSSL.Checked Then
            sProtocol = "wss"
        End If

        Dim sUrl As String = sProtocol & "://" & sServer & "/Handler1.ashx?user=" & System.Web.HttpUtility.UrlEncode(txtUser.Text)

        oSocket = New System.Net.WebSockets.ClientWebSocket

        Try
            Await oSocket.ConnectAsync(New Uri(sUrl), Nothing)
        Catch ex As Exception
            MsgBox("Could not connect.  Please try another name. " & ex.Message & ", URL: " & sUrl)
            Exit Sub
        End Try

        btnOpen.Enabled = False
        btnSend.Enabled = True
        btnClose.Enabled = True
        lbStatus.ForeColor = Color.FromArgb(0, 255, 0)

        Const iMaxBufferSize As Integer = 64 * 1024
        Dim buffer = New Byte(iMaxBufferSize - 1) {}

        While oSocket.State = WebSocketState.Open
            'Get Msg
            Dim result = Await oSocket.ReceiveAsync(New ArraySegment(Of Byte)(buffer), Threading.CancellationToken.None)
            If result.MessageType = WebSocketMessageType.Text Then
                Dim oBytes As Byte() = New Byte(result.Count - 1) {}
                Array.Copy(buffer, oBytes, result.Count)
                Dim oFinalBuffer As List(Of Byte) = New List(Of Byte)()
                oFinalBuffer.AddRange(oBytes)

                'Get Remaining Msg
                While result.EndOfMessage = False
                    result = Await oSocket.ReceiveAsync(New ArraySegment(Of Byte)(buffer), Threading.CancellationToken.None)
                    oBytes = New Byte(result.Count - 1) {}
                    Array.Copy(buffer, oBytes, result.Count)
                    oFinalBuffer.AddRange(oBytes)
                End While

                Dim sMsg As String = System.Text.Encoding.UTF8.GetString(oFinalBuffer.ToArray())

                If sMsg = "{{RefreshUsers}}" Then
                    RefreshUsers()
                Else
                    If txtOutput.Text <> "" Then txtOutput.Text += vbCrLf
                    txtOutput.Text += sMsg
                End If
            End If
        End While

        LogOff()
    End Sub

    Private Async Sub btnSend_Click(sender As Object, e As EventArgs) Handles btnSend.Click
        If IsNothing(oSocket) OrElse oSocket.State <> WebSocketState.Open Then
            Exit Sub
        End If

        Dim sMsg As String = txtMsg.Text
        If sMsg = "" Then
            Exit Sub
        End If

        txtMsg.Text = ""

        Await oSocket.SendAsync(New ArraySegment(Of Byte)(System.Text.Encoding.UTF8.GetBytes(sMsg)),
                            System.Net.WebSockets.WebSocketMessageType.Text, True, Nothing)
    End Sub

    Private Sub btnClose_Click(sender As Object, e As EventArgs) Handles btnClose.Click
        LogOff()
    End Sub

    Private Async Sub LogOff()

        btnOpen.Enabled = True
        btnSend.Enabled = False
        btnClose.Enabled = False
        lbStatus.ForeColor = Color.FromArgb(255, 0, 0)
        selUsers.Items.Clear()

        If IsNothing(oSocket) OrElse oSocket.State <> WebSocketState.Open Then
            Exit Sub
        End If

        Await oSocket.CloseAsync(WebSocketCloseStatus.Empty, "", Threading.CancellationToken.None)
    End Sub

    Private Sub btnRefreshUsers_Click(sender As Object, e As EventArgs) Handles btnRefreshUsers.Click
        RefreshUsers()
    End Sub

    Private Sub RefreshUsers()
        selUsers.Items.Clear()

        Dim sServer As String = txtURL.Text 'localhost/WebSocket
        If sServer = "" Then
            MsgBox("Server URL is missing")
            Exit Sub
        End If

        Dim sProtocol As String = "http"
        If chkSSL.Checked Then
            sProtocol = "https"
        End If

        Dim sUrl As String = sProtocol & "://" & sServer & "/Chat.aspx?getUsers2=1"
        Dim sUsers As String = GetData(sUrl)
        Dim oUsers As String() = sUsers.Split(vbCrLf)

        For i = 0 To oUsers.Length - 1
            Dim sUser As String = oUsers(i)
            selUsers.Items.Add(sUser)
        Next
    End Sub

    Private Function GetData(ByVal sUrl As String) As String
        Dim oHttpWebRequest As System.Net.HttpWebRequest
        Dim oHttpWebResponse As System.Net.HttpWebResponse

        oHttpWebRequest = CType(System.Net.WebRequest.Create(sUrl), System.Net.HttpWebRequest)
        oHttpWebRequest.Timeout = 1000 * 60 * 60 'Hour
        oHttpWebRequest.KeepAlive = False
        oHttpWebRequest.Method = "POST"
        oHttpWebRequest.ContentLength = 0

        Try
            oHttpWebResponse = CType(oHttpWebRequest.GetResponse(), System.Net.HttpWebResponse)
        Catch ex As Exception
            Return ex.Message & vbCrLf & ex.StackTrace()
        End Try

        'Read Request
        Dim oStreamReader As IO.StreamReader = New IO.StreamReader(oHttpWebResponse.GetResponseStream, System.Text.UTF8Encoding.UTF8)
        Dim sHTML As String = oStreamReader.ReadToEnd

        oStreamReader.Close()
        oHttpWebResponse.Close()
        oHttpWebRequest.Abort()

        Return sHTML
    End Function

    Private Sub Form1_Closing(sender As Object, e As CancelEventArgs) Handles Me.Closing
        LogOff()
    End Sub

    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load

    End Sub
End Class
