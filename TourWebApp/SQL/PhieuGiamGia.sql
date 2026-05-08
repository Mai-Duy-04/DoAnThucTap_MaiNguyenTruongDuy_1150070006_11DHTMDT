/* =============================================================
   Module: PhieuGiamGia
   HappyTrip - SQL script tao bang + cot lien quan voucher
   Chay script nay tren DB hien tai (SQL Server)
   ============================================================= */

SET NOCOUNT ON;
GO

IF OBJECT_ID('dbo.PhieuGiamGia', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.PhieuGiamGia
    (
        IdPhieuGiamGia INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        MaPhieu NVARCHAR(50) NOT NULL,
        TenPhieu NVARCHAR(200) NOT NULL,
        LoaiGiam NVARCHAR(20) NOT NULL CONSTRAINT DF_PhieuGiamGia_LoaiGiam DEFAULT('PhanTram'),
        GiaTriGiam DECIMAL(18,2) NOT NULL,
        DonToiThieu DECIMAL(18,0) NOT NULL CONSTRAINT DF_PhieuGiamGia_DonToiThieu DEFAULT(0),
        GiamToiDa DECIMAL(18,0) NULL,
        NgayBatDau DATETIME NULL,
        NgayKetThuc DATETIME NULL,
        TongLuotSuDung INT NULL,
        DaSuDung INT NOT NULL CONSTRAINT DF_PhieuGiamGia_DaSuDung DEFAULT(0),
        LuotToiDaMoiTaiKhoan INT NULL,
        PhamViApDung NVARCHAR(20) NOT NULL CONSTRAINT DF_PhieuGiamGia_PhamVi DEFAULT('TatCa'),
        IdTour INT NULL,
        IdLoaiTour INT NULL,
        TrangThai BIT NOT NULL CONSTRAINT DF_PhieuGiamGia_TrangThai DEFAULT(1),
        MoTa NVARCHAR(500) NULL,
        NgayTao DATETIME NOT NULL CONSTRAINT DF_PhieuGiamGia_NgayTao DEFAULT(GETDATE()),
        NgayCapNhat DATETIME NULL
    );

    CREATE UNIQUE INDEX UX_PhieuGiamGia_Ma ON dbo.PhieuGiamGia(MaPhieu);
    CREATE INDEX IX_PhieuGiamGia_TrangThai ON dbo.PhieuGiamGia(TrangThai);

    ALTER TABLE dbo.PhieuGiamGia WITH CHECK ADD CONSTRAINT FK_PGG_Tour
        FOREIGN KEY (IdTour) REFERENCES dbo.Tour(IdTour);

    ALTER TABLE dbo.PhieuGiamGia WITH CHECK ADD CONSTRAINT FK_PGG_LoaiTour
        FOREIGN KEY (IdLoaiTour) REFERENCES dbo.LoaiTour(IdLoaiTour);
END
GO

IF OBJECT_ID('dbo.PhieuGiamGiaSuDung', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.PhieuGiamGiaSuDung
    (
        IdSuDung INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        IdPhieuGiamGia INT NOT NULL,
        IdDon INT NOT NULL,
        IdTaiKhoan INT NOT NULL,
        MaPhieu NVARCHAR(50) NOT NULL,
        SoTienGiam DECIMAL(18,0) NOT NULL,
        ThoiDiemSuDung DATETIME NOT NULL CONSTRAINT DF_PGGSD_ThoiDiem DEFAULT(GETDATE()),
        TrangThai NVARCHAR(20) NOT NULL CONSTRAINT DF_PGGSD_TrangThai DEFAULT('GiuCho'),
        GhiChu NVARCHAR(500) NULL
    );

    CREATE INDEX IX_PGGSD_Phieu ON dbo.PhieuGiamGiaSuDung(IdPhieuGiamGia);
    CREATE INDEX IX_PGGSD_Don ON dbo.PhieuGiamGiaSuDung(IdDon);
    CREATE INDEX IX_PGGSD_TaiKhoan ON dbo.PhieuGiamGiaSuDung(IdTaiKhoan);

    ALTER TABLE dbo.PhieuGiamGiaSuDung WITH CHECK ADD CONSTRAINT FK_PGGSD_PGG
        FOREIGN KEY (IdPhieuGiamGia) REFERENCES dbo.PhieuGiamGia(IdPhieuGiamGia);

    ALTER TABLE dbo.PhieuGiamGiaSuDung WITH CHECK ADD CONSTRAINT FK_PGGSD_Don
        FOREIGN KEY (IdDon) REFERENCES dbo.DonDatTour(IdDon);

    ALTER TABLE dbo.PhieuGiamGiaSuDung WITH CHECK ADD CONSTRAINT FK_PGGSD_TaiKhoan
        FOREIGN KEY (IdTaiKhoan) REFERENCES dbo.TaiKhoan(IdTaiKhoan);
END
GO

IF COL_LENGTH('dbo.DonDatTour', 'IdPhieuGiamGia') IS NULL
BEGIN
    ALTER TABLE dbo.DonDatTour ADD IdPhieuGiamGia INT NULL;
END
GO

IF COL_LENGTH('dbo.DonDatTour', 'MaPhieuGiamGia') IS NULL
BEGIN
    ALTER TABLE dbo.DonDatTour ADD MaPhieuGiamGia NVARCHAR(50) NULL;
END
GO

IF COL_LENGTH('dbo.DonDatTour', 'SoTienGiam') IS NULL
BEGIN
    ALTER TABLE dbo.DonDatTour ADD SoTienGiam DECIMAL(18,0) NULL;
END
GO

IF COL_LENGTH('dbo.DonDatTour', 'TongTienSauGiam') IS NULL
BEGIN
    ALTER TABLE dbo.DonDatTour ADD TongTienSauGiam DECIMAL(18,0) NULL;
END
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = 'FK_Don_PhieuGiamGia'
      AND parent_object_id = OBJECT_ID('dbo.DonDatTour')
)
BEGIN
    ALTER TABLE dbo.DonDatTour WITH CHECK ADD CONSTRAINT FK_Don_PhieuGiamGia
        FOREIGN KEY (IdPhieuGiamGia) REFERENCES dbo.PhieuGiamGia(IdPhieuGiamGia);
END
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_DonDatTour_IdPhieuGiamGia'
      AND object_id = OBJECT_ID('dbo.DonDatTour')
)
BEGIN
    CREATE INDEX IX_DonDatTour_IdPhieuGiamGia ON dbo.DonDatTour(IdPhieuGiamGia);
END
GO

IF OBJECT_ID('dbo.PhieuGiamGiaTaiKhoan', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.PhieuGiamGiaTaiKhoan
    (
        IdLuu INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        IdTaiKhoan INT NOT NULL,
        IdPhieuGiamGia INT NOT NULL,
        NgayLuu DATETIME NOT NULL CONSTRAINT DF_PGGTK_NgayLuu DEFAULT(GETDATE()),
        TrangThai BIT NOT NULL CONSTRAINT DF_PGGTK_TrangThai DEFAULT(1)
    );

    CREATE UNIQUE INDEX UX_PGGTK_UserVoucher ON dbo.PhieuGiamGiaTaiKhoan(IdTaiKhoan, IdPhieuGiamGia);
    CREATE INDEX IX_PGGTK_User ON dbo.PhieuGiamGiaTaiKhoan(IdTaiKhoan);

    ALTER TABLE dbo.PhieuGiamGiaTaiKhoan WITH CHECK ADD CONSTRAINT FK_PGGTK_TK
        FOREIGN KEY (IdTaiKhoan) REFERENCES dbo.TaiKhoan(IdTaiKhoan);

    ALTER TABLE dbo.PhieuGiamGiaTaiKhoan WITH CHECK ADD CONSTRAINT FK_PGGTK_PGG
        FOREIGN KEY (IdPhieuGiamGia) REFERENCES dbo.PhieuGiamGia(IdPhieuGiamGia);
END
GO
