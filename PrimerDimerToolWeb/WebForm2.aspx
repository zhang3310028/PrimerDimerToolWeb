<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="WebForm2.aspx.cs" Inherits="PrimerDimerToolWeb.WebForm2" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
    <%@import Namespace="PrimerDimerToolWeb"%>
<head runat="server">
<meta http-equiv="Content-Type" content="text/html; charset=utf-8"/>
    <title></title>
        <script type="text/javascript">
            var guid = "<asp:Literal id='G' runat='server'/>";
            var ajax = window.XMLHttpRequest ? new window.XMLHttpRequest() : new ActiveXObject("Msxml2.XMLHTTP");
            var timer = null;
            function getProcess() {
                ajax.open("GET", "<%=Request.Url.ToString() %>?t="+Date.parse(new Date())+"&guid="+guid, true);
                //ajax.open("GET", "<%=Request.Url.ToString() %>?t="+Date.parse(new Date()), true);
                ajax.onreadystatechange = function () {
                    if (ajax.status == 200 && ajax.readyState == 4) {
                        if (ajax.responseText == "complete") {
                            window.clearInterval(timer);
                            document.getElementById("Label2").innerHTML = "complete!";
                        } else if (ajax.responseText == "") {

                        }else{
                            document.getElementById("Label2").innerHTML = ajax.responseText;
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
        <asp:UpdatePanel ID="UpdatePanel1" runat="server">
            <ContentTemplate>
                <asp:Label ID="Label2" runat="server" Text=" "></asp:Label>

            </ContentTemplate>
            
        </asp:UpdatePanel>
        <br />
            
        <div>
            <p>
                <%=getResultLink() %>
            </p>
        </div>
        <br />
        
    </div>
    </form>
</body>
</html>
