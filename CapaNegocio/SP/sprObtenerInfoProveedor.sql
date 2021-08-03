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
-- Create date: 03/08/2021
-- Description:	Obtiene la información del proveedor asociado a un id
-- =============================================
CREATE PROCEDURE sprObtenerInfoProveedor
	@idProveedor int
AS
BEGIN
	SELECT ID_PROVIDER as idProveedor, PROVIDER as nombreProveedor, Brand as marca, Issuer as issuer, ID_Master as idMaster
	FROM PROVIDERS
	WHERE ID_PROVIDER=@idProveedor
	AND Status=1
END
GO
