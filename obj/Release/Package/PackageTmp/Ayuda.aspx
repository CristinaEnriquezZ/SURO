<%@ Page Title="" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Ayuda.aspx.cs" Inherits="SURO2.Ayuda" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
       <title>Ayuda</title>
    <link rel="stylesheet" href="https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.0.0-beta3/css/all.min.css"/>
    <link rel="stylesheet" href="css/Ayuda.css" />
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="cphContenido" runat="server">

    <div class="ayuda-container">
        <h1>¿Necesitas ayuda?</h1>
        <p>Estamos aquí para asistirte. Puedes contactarnos a través de los siguientes medios:</p>

        <div class="ayuda-contacto">

            <div class="contacto-item">
                <div class="icono">
                    <i class="fas fa-envelope"></i>
                </div>
                <div class="texto">
                    <h3>Mesa de ayuda</h3>
                    <a href="http://10.18.24.11/mesaayuda">Ir a mesa de ayuda</a>
                </div>

            </div>

            <div class="contacto-item">
                <div class="icono">
                    <i class="fas fa-phone-alt"></i>
                </div>
                <div class="texto">
                    <h3>Ext</h3>
                    12566
                </div>
            </div>

        </div>
    </div>
</asp:Content>
