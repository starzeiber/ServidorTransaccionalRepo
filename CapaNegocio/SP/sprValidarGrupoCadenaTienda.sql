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
-- Description:	Obtiene coincidencia con los valores de grupo, cadena y tienda
-- =============================================
CREATE PROCEDURE sprValidarGrupoCadenaTienda
	@idGrupo int,
	@idCadena int,
	@idTienda int
AS
BEGIN
	SELECT ID_MERCHANT_PK
	FROM MERCHANT 
	WHERE ID_GRP=@idGrupo 
	AND ID_CHAIN=@idCadena 
	AND ID_MERCHANT=@idTienda 
	AND STATUS=1
END
GO
