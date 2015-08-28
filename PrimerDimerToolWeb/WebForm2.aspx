<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="WebForm2.aspx.cs" Inherits="PrimerDimerToolWeb.WebForm2" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
    <%@import Namespace="PrimerDimerToolWeb"%>
<head runat="server">
<meta http-equiv="Content-Type" content="text/html; charset=utf-8"/>
    <title></title>
        <script type="text/javascript">
            var guid = "<asp:Literal id='guid' runat='server'/>";
            var ajax = window.XMLHttpRequest ? new window.XMLHttpRequest() : new ActiveXObject("Msxml2.XMLHTTP");
            var timer = null;
            function getProcess() {
                ajax.open("GET", "<%=Request.Url.ToString() %>?t="+Date.parse(new Date())+"&guid="+guid, true);
                //ajax.open("GET", "<%=Request.Url.ToString() %>?t="+Date.parse(new Date()), true);
                ajax.onreadystatechange = function () {
                    if (ajax.status == 200 && ajax.readyState == 4) {
                        if (ajax.responseText.startsWith("<a href=")) {
                            window.clearInterval(timer);
                            document.getElementById("Label2").innerHTML = ajax.responseText;
                            document.getElementById("processbar1").innerHTML = "";
                            document.getElementById("processbar2").style.width = "100%";
                        } else if (ajax.responseText == "") {

                        } else {
                            data = ajax.responseText;
                            if (data != null) {
                                var task = eval('(' + data + ')');
                                var waitQueue = task['waitQueue'];
                                var percent = task['percent'];
                                var status = task['status'];
                                if (waitQueue != 0) {
                                    document.getElementById("processbar1").innerHTML = "waiting "+waitQueue+" task";
                                }else if (percent !=0) {
                                    document.getElementById("processbar1").innerHTML = status;
                                    document.getElementById("processbar2").style.width = percent+"%";
                                }
                            }
                        }
                    }
                }
          
                ajax.send(null)
            }
            timer = window.setInterval("getProcess()", 1000)
        </script>
</head>
<body>
    <form id="form1" runat="server">
        <asp:ScriptManager ID="ScriptManager1" runat="Server" ></asp:ScriptManager>
        <div style="text-align: center;">
        <h2>Primer Dimer Tool</h2>
        
        <asp:Label ID="Label1" runat="server" Text="Primer File:">
            
        </asp:Label>
        <asp:FileUpload ID="FileUpload1" runat="server" />
        <br />
        <asp:Button ID="Button1" runat="server" OnClick="Button1_Click" Text="提交" />
        <div id="processbar" style="border: 1px solid blue; width: 600px; position: relative;margin:10px 0;">
             <div style="background: #00ff21; width: 0; height: 20px;" id="processbar2"></div>
             <div style="position: absolute; text-align: center; top: 0; width: 100%" id="processbar1"></div>
        </div>
        <br />
        <asp:UpdatePanel ID="UpdatePanel1" runat="server">
            <ContentTemplate>
                <asp:Label ID="Label2" runat="server" Text=" "></asp:Label>
            </ContentTemplate>
        </asp:UpdatePanel>
        
        
    </div>
    </form>
</body>
    <script type="text/javascript">
        function doMargin() {
            var divname = document.getElementById("processbar");
            divname.style.left = ((document.body.clientWidth - divname.clientWidth) / 2) + "px";
        }
        doMargin();
    </script>
</html>
