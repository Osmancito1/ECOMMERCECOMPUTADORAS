<%@ Page Title="Mapa del Sitio" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="MapaSitio.aspx.cs" Inherits="EcommerceComputadorasNW.MapaSitio" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
    <style>
        .sitemap-container {
            padding: 30px;
            max-width: 800px;
            margin: 0 auto;
            background-color: #f9f9f9;
            border-radius: 12px;
            box-shadow: 0 0 10px rgba(0,0,0,0.1);
        }
        .sitemap-container h2 {
            margin-bottom: 20px;
        }
    </style>
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
    <div class="sitemap-container">
        <h2>Mapa del Sitio</h2>

        <asp:SiteMapPath ID="SiteMapPath1" runat="server" PathSeparator=" > " />
        <br /><br />

        <asp:Menu ID="Menu1" runat="server" DataSourceID="SiteMapDataSource1" Orientation="Vertical"
                  StaticDisplayLevels="3"
                  Font-Names="Segoe UI"
                  Font-Size="Medium"
                  ForeColor="#333"
                  StaticMenuStyle-BackColor="White"
                  StaticMenuStyle-BorderStyle="None"
                  StaticMenuItemStyle-HorizontalPadding="10px"
                  StaticMenuItemStyle-VerticalPadding="5px">
        </asp:Menu>

        <asp:SiteMapDataSource ID="SiteMapDataSource1" runat="server" />
    </div>
</asp:Content>
