-- ================================================
-- Template generated from Template Explorer using:
-- Create Procedure (New Menu).SQL
--
-- Use the Specify Values for Template Parameters 
-- command (Ctrl-Shift-M) to fill in the parameter 
-- values below.
--
-- This block of comments will not be included in
-- the definition of the procedure.
-- ================================================
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		Uriel Martínez
-- Create date: 02/08/2021
-- Description:	Obtiene la información del producto asociado a un SKU
-- =============================================
CREATE PROCEDURE sprObtenerInfoProducto 
	@sku varchar(13)
AS
BEGIN
	SELECT ID_PROVIDER AS idProveedor,Msg_ticket1 as mensajeTicket1, Msg_ticket2 as mensajeTicket2, Price as monto,BarCode as idPaquete, Brand as nombreProveedor
	FROM PRODUCT
	WHERE ProductID=@sku
	AND Status=1
END
GO
