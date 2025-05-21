<%@ Page Language="VB"%>
<% 
    Dim sUserList As String = ""
    Dim sUserList2 As String = ""

    If Application("Sockets") IsNot Nothing Then
        For Each oSocket As DictionaryEntry In Application("Sockets")
            Dim sUser As String = oSocket.Key
            sUserList += "<option value=""" & sUser & """>" & sUser & "</option>" & vbCrLf

            If sUserList2 <> "" Then sUserList2 += vbCrLf
            sUserList2 += sUser
        Next
    End If

    If Request.QueryString("getUsers") = "1" Then
        Response.Write(sUserList)
        Response.End()

    ElseIf Request.QueryString("getUsers2") = "1" Then
        Response.Write(sUserList2)
        Response.End()

    ElseIf Request.QueryString("resetUsers") = "1" Then

        If Application("Sockets") IsNot Nothing Then
            For Each oEntry As DictionaryEntry In Application("Sockets")
                Dim oSocket As Object = oEntry.Value

                Try
                    oSocket.CloseOutputAsync(System.Net.WebSockets.WebSocketCloseStatus.NormalClosure, "", System.Threading.CancellationToken.None)
                    oSocket.CloseAsync(System.Net.WebSockets.WebSocketCloseStatus.NormalClosure, "", System.Threading.CancellationToken.None)
                Catch ex As Exception
                    'System.Threading.Thread.Sleep(1000)
                End Try

            Next

            Application("Sockets") = Nothing
        End If

        Response.Write("Users reseted")
        Response.End()

    End If
%>
<!DOCTYPE html>
<html>
    <head>
        <title>Chat App</title>
    </head>
    <body>
        <script>
            var websocket = null;

            function Open() {

                var sProtocol = window.location.protocol == "https:" ? "wss" : "ws";
                var uri = sProtocol + '://' + window.location.hostname + "/WebSocket/Handler1.ashx?user=" + escape(txtUser.value);
                websocket = new WebSocket(uri);           

                websocket.onopen = function () {
                    //Connected   
                    btnSend.disabled = false; 
                    btnClose.disabled = false; 
                    btnOpen.disabled = true; 
                    spStatus.style.color = "green";
                    RefreshUsers();
                };

                websocket.onclose = function () {
                    if (document.readyState == "complete") {
                        //Connection lost
                        btnSend.disabled = true;
                        btnClose.disabled = true; 
                        btnOpen.disabled = false; 
                        spStatus.style.color = "red";
                        selOtherUsers.length = 0;
                        RefreshUsers();
                    }
                };

                websocket.onmessage = function (event) {
                    if (event.data == "{{RefreshUsers}}") {
                        RefreshUsers();
                        return;
                    }

                    if (txtOutput.value != "") txtOutput.value += "\n";
                    txtOutput.value += event.data;
                };   

                websocket.onerror = function (event) {
                    alert('Could not connect.  Please try another name.');
                };   

                setTimeout(function () { RefreshUsers() }, 1000);
            }

            function Send() {
                if (txtMsg.value == "") return;
                websocket.send(txtMsg.value);
                txtMsg.value = "";
            }

            function Close() {
                websocket.close();
            }

            function RefreshUsers() {
                var oHttp = new XMLHttpRequest();
                oHttp.open("POST", "?getUsers=1", false);
                oHttp.setRequestHeader("Content-Type", "application/x-www-form-urlencoded");
                oHttp.onreadystatechange = function () { // Call a function when the state changes.
                    if (this.readyState === XMLHttpRequest.DONE && this.status === 200) {
                        selOtherUsers.innerHTML = oHttp.responseText;
                    }
                }
                oHttp.send();
            }
            
        </script>

        <div>
            <label for="txtUser">User Name</label>
            <input id="txtUser" value="Jack" />

            <button type="button" onclick="Open()" id="btnOpen">Login</button>
            <button type="button" onclick="Close()" id="btnClose" disabled>Log off</button>

            <span id="spStatus" style="color:red">⬤</span>

            <div style="float: right">
                <label for="selOtherUsers">Users</label>

                <button type="button" onclick="RefreshUsers()">&#x21bb;</button>
            </div>
        </div>

        <div>
            <table style="width: 100%; height: 300px">
                <tr>
                    <td style="width: 78%; height: 100%">
                        <textarea id="txtOutput" rows="1" style="margin-top: 10px; width: 100%; height: 100%" placeholder="Output"></textarea>
                    </td>
                    <td style="width: 20%; height: 100%; padding-left: 10px">
                        <select id="selOtherUsers" style="width: 100%; height: 100%" multiple>
                            <%=sUserList%>
                        </select>
                    </td>
                </tr>
            </table>
            
        </div>        

        <div>
            <textarea id="txtMsg" rows="5" wrap="soft" style="width: 98%; margin-left: 3px; margin-top: 6px" placeholder="Input Text"></textarea>
        </div>   

        <button type="button" onclick="Send()" id="btnSend" disabled>Send</button>  
    </body>
</html>
