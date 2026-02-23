<%@ Page Title="" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Login.aspx.cs" Inherits="SURO2.Login" %>



<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
    <title>Login-SURO</title>

    <!-- Custom fonts for this template-->
    <link href="vendor/fontawesome-free/css/all.min.css" rel="stylesheet" type="text/css"/>
    <link
        href="https://fonts.googleapis.com/css?family=Nunito:200,200i,300,300i,400,400i,600,600i,700,700i,800,800i,900,900i"
        rel="stylesheet"/>

    <!-- Custom styles for this template-->
    <link href="css/sb-admin-2.min.css" rel="stylesheet"/>

    <!-- Bootstrap core JavaScript-->
    <script src="vendor/jquery/jquery.min.js"></script>
    <script src="vendor/bootstrap/js/bootstrap.bundle.min.js"></script>

    <!-- Core plugin JavaScript-->
    <script src="vendor/jquery-easing/jquery.easing.min.js"></script>

    <!-- Custom scripts for all pages-->
    <script src="js/sb-admin-2.min.js"></script>


    <script>         
        document.addEventListener("DOMContentLoaded", function () {
            if (!sessionStorage.getItem("loginAnimatedOnce")) {
                cument.body.classList.add("login-ready");
                ssionStorage.setItem("loginAnimatedOnce", "true");
            } else {
                  // Salta la animación, muestra estático
                cument.querySelector(".login-container").style.opacity = "1";
                cument.querySelector(".login-container").style.transform = "scale(1)";

            };
    </script>

    <style>
      

.login-box .left {
    display: flex;
    align-items: center;
    justify-content: center;
    padding: 2rem;
}

.login-box{
    width: 90%;
max-width: 900px;
}
    </style>

</asp:Content>



<asp:Content ID="Content2" ContentPlaceHolderID="cphContenido" runat="server">


    <div class="login-container">
      
        <div class="login-box">
              <div class="top-left-logo">
                <img src="img/SDR.png" alt="Secretaría de Desarrollo Rural"/>
            </div>
            <div class="left d-flex align-items-center justify-content-center">
                <img src="img/suro4.png" style="max-width: 100%; height: auto;" alt="SURO" />
            </div>
            <div class="right">
                <h4 class="text-center font-weight-bold mb-4">Bienvenido</h4>
                <div class="form-group">
                    <asp:TextBox ID="txtUser" runat="server" CssClass="form-control" placeholder="Usuario" />
                </div>
                <div class="form-group">
                    <asp:TextBox ID="txtPass" runat="server" CssClass="form-control" placeholder="Contraseña" TextMode="Password" />
                </div>
                <div class="form-check mb-3">
                    <asp:Label ID="lblMensaje" runat="server"  CssClass="text-danger text-start d-block"></asp:Label>
                    <br />
                    <br />
                   <%--<input type="checkbox" class="form-check-input" id="chkRecordar" name="chkRecordar" />
                    <label class="form-check-label" for="chkRecordar">Olvidé mi contraseña</label>--%>
                    <div class="mb-3 text-center">
                      <a href="#" data-toggle="modal" data-target="#modalRecuperar" style="font-size: 15px;">¿Olvidaste tu contraseña?</a>
                    </div>

                </div>
                <asp:Button ID="btnEntrar" OnClick="btnEntrar_Click" runat="server"  Text="Entrar" CssClass="btn btn-primary btn-block" />
            </div>
        </div>
    </div>

     <!-- Modal Recuperar Contraseña -->
       <div class="modal fade" id="modalRecuperar" tabindex="-1" role="dialog" aria-labelledby="modalRecuperarLabel" aria-hidden="true">
      <div class="modal-dialog" role="document">
        <div class="modal-content">
          <div class="modal-header">
            <h5 class="modal-title" id="modalRecuperarLabel">Recuperar Contraseña</h5>
            <button type="button" class="close" data-dismiss="modal" aria-label="Cerrar">
              <span aria-hidden="true">&times;</span>
            </button>
          </div>
          <div class="modal-body">
            <asp:Label ID="lblModalMensaje" runat="server" CssClass="text-danger"></asp:Label>
            <div class="form-group">
              <asp:TextBox ID="txtUsuarioRecuperar" runat="server" CssClass="form-control" placeholder="Usuario" />
            </div>
            <div class="form-group">
             <asp:Button ID="btnEnviarCodigo" runat="server" ClientIDMode="Static"
                            CssClass="btn btn-secondary btn-block"
                            Text="Enviar Código"
                            OnClick="btnEnviarCodigo_Click" />
            </div>
            <hr />
            <div class="form-group">
              <asp:TextBox ID="txtCodigo" runat="server" CssClass="form-control" placeholder="Código de verificación" />
            </div>
            <div class="form-group">
              <asp:TextBox ID="txtNuevaPass" runat="server" CssClass="form-control" placeholder="Nueva contraseña" TextMode="Password" />
            </div>
            <asp:Button ID="btnCambiarPass" runat="server" CssClass="btn btn-primary btn-block" Text="Cambiar contraseña" OnClick="btnCambiarPass_Click" />
          </div>
        </div>
      </div>
    </div>

  <script>
            function iniciarTimerCodigo() {
                var btn = document.getElementById('btnEnviarCodigo');
                var textoOriginal = btn.value;
                var segundos = 15;
                btn.disabled = true;
                btn.value = "Espera " + segundos + "s...";

                var intervalo = setInterval(function () {
                    segundos--;
                    btn.value = "Espera " + segundos + "s...";
                    if (segundos <= 0) {
                        clearInterval(intervalo);
                        btn.disabled = false;
                        btn.value = textoOriginal;
                    }
                }, 1000);
            }
  </script>

</asp:Content>





