<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Confirmacion.aspx.cs" Inherits="EcommerceComputadorasNW.Confirmacion" %>

<!DOCTYPE html>
<html lang="es">
<head runat="server">
    <meta charset="UTF-8">
    <title>Confirmación de Pedido - TechStore</title>
    <link rel="stylesheet" href="Content/Confirmacion.css">
</head>
<body>
    <form id="form1" runat="server">
        <div class="container">
            <div class="confirmation-box">
                <div class="success-icon">
                    <i class="fas fa-check-circle"></i>
                </div>
                <h1>¡Gracias por tu compra!</h1>
                <p>Tu pedido ha sido procesado exitosamente. Hemos enviado un correo de confirmación a tu dirección.</p>
                
                <div class="order-summary">
                    <h2>Resumen del Pedido #<asp:Literal ID="ltlNumeroPedido" runat="server" /></h2>
                    <div class="order-details">
                        <p><strong>Fecha:</strong> <asp:Literal ID="ltlFechaPedido" runat="server" /></p>
                        <p><strong>Cliente:</strong> <asp:Literal ID="ltlNombreCliente" runat="server" /></p>
                        <p><strong>Dirección de Envío:</strong><br /><asp:Literal ID="ltlDireccionEnvio" runat="server" /></p>
                    </div>

                    <div class="order-items">
                        <asp:Repeater ID="rptProductosConfirmacion" runat="server">
                            <HeaderTemplate>
                                <table class="products-table">
                                    <thead><tr><th>Producto</th><th>Cantidad</th><th>Precio</th><th>Subtotal</th></tr></thead>
                                    <tbody>
                            </HeaderTemplate>
                            <ItemTemplate>
                                <tr>
                                    <td><%# Eval("NomPro") %></td>
                                    <td><%# Eval("CantPro") %></td>
                                    <td><%# Eval("PreUniPro", "{0:C}") %></td>
                                    <td><%# Eval("Subtotal", "{0:C}") %></td>
                                </tr>
                            </ItemTemplate>
                            <FooterTemplate>
                                    </tbody>
                                </table>
                            </FooterTemplate>
                        </asp:Repeater>
                    </div>
                    
                    <div class="order-totals">
                        <div class="total-line"><span>Subtotal:</span><span><asp:Literal ID="ltlSubtotal" runat="server" /></span></div>
                        <div class="total-line"><span>Envío:</span><span><asp:Literal ID="ltlEnvio" runat="server" /></span></div>
                        <div class="total-line"><span>ISV (15%):</span><span><asp:Literal ID="ltlImpuestos" runat="server" /></span></div>
                        <div class="total-line total"><span>Total Pagado:</span><span><asp:Literal ID="ltlTotal" runat="server" /></span></div>
                    </div>
                </div>

                <div class="actions">
                    <asp:Button ID="btnImprimirFactura" runat="server" Text="Imprimir Factura" CssClass="btn-primary" OnClick="btnImprimirFactura_Click" />
                    <a href="Default.aspx" class="btn-secondary">Continuar Comprando</a>
                </div>
            </div>
        </div>
    </form>
    <script src="https://cdn.jsdelivr.net/npm/canvas-confetti@1.5.1/dist/confetti.browser.min.js"></script>
    <script>
        window.onload = () => {
            confetti({
                particleCount: 100,
                spread: 70,
                origin: { y: 0.6 }
            });
        };
    </script>
</body>
</html>