CREATE OR ALTER VIEW dbo.VendorPortalPoInvoicePaymentStatusView
AS
WITH RrpoByPo AS
(
    SELECT
        PONo,
        SuppCode,
        MAX(TransNmbr) AS RrpoNo,
        MAX(CASE WHEN ISNULL(Status, '') = 'P' THEN 1 ELSE 0 END) AS HasPostedRrpo,
        MAX(CASE WHEN ISNULL(DoneInvoice, 'N') = 'Y' THEN 1 ELSE 0 END) AS HasDoneInvoiceFlag
    FROM dbo.STCRRPOHd
    WHERE NULLIF(NULLIF(LTRIM(RTRIM(PONo)), ''), '-') IS NOT NULL
      AND ISNULL(Status, '') <> 'D'
    GROUP BY PONo, SuppCode
),
InvoiceSource AS
(
    SELECT DISTINCT
        NULLIF(NULLIF(LTRIM(RTRIM(h.PONo)), ''), '-') AS PONo,
        h.Supplier AS SuppCode,
        h.TransNmbr AS InvoiceNo,
        h.Status AS InvoiceStatus
    FROM dbo.FInsuppINVhd h
    WHERE NULLIF(NULLIF(LTRIM(RTRIM(h.PONo)), ''), '-') IS NOT NULL
      AND ISNULL(h.Status, '') <> 'D'

    UNION

    SELECT DISTINCT
        COALESCE(
            NULLIF(NULLIF(LTRIM(RTRIM(d.PONo)), ''), '-'),
            NULLIF(NULLIF(LTRIM(RTRIM(rr.PONo)), ''), '-'),
            NULLIF(NULLIF(LTRIM(RTRIM(h.PONo)), ''), '-')
        ) AS PONo,
        COALESCE(rr.SuppCode, h.Supplier) AS SuppCode,
        h.TransNmbr AS InvoiceNo,
        h.Status AS InvoiceStatus
    FROM dbo.FInsuppINVDt d
    INNER JOIN dbo.FInsuppINVhd h ON h.TransNmbr = d.TransNmbr
    LEFT JOIN dbo.STCRRPOHd rr ON rr.TransNmbr = d.ReffNmbr
        AND ISNULL(rr.Status, '') <> 'D'
    WHERE COALESCE(
            NULLIF(NULLIF(LTRIM(RTRIM(d.PONo)), ''), '-'),
            NULLIF(NULLIF(LTRIM(RTRIM(rr.PONo)), ''), '-'),
            NULLIF(NULLIF(LTRIM(RTRIM(h.PONo)), ''), '-')
        ) IS NOT NULL
      AND ISNULL(h.Status, '') <> 'D'
),
InvoiceByPo AS
(
    SELECT
        PONo,
        SuppCode,
        MAX(InvoiceNo) AS InvoiceNo,
        MAX(InvoiceStatus) AS InvoiceStatus,
        MAX(CASE WHEN NULLIF(ISNULL(InvoiceNo, ''), '') IS NOT NULL THEN 1 ELSE 0 END) AS HasInvoice,
        MAX(CASE WHEN ISNULL(InvoiceStatus, '') = 'P' THEN 1 ELSE 0 END) AS HasPostedInvoice
    FROM InvoiceSource
    WHERE PONo IS NOT NULL
    GROUP BY PONo, SuppCode
),
ApprovalSource AS
(
    SELECT DISTINCT
        COALESCE(
            NULLIF(NULLIF(LTRIM(RTRIM(d.PONo)), ''), '-'),
            inv.PONo
        ) AS PONo,
        COALESCE(inv.SuppCode, h.SuppCode) AS SuppCode,
        h.TransNmbr AS ApprovalNo,
        h.Status AS ApprovalStatus,
        h.DonePayment AS ApprovalDonePayment
    FROM dbo.FINAPApprovalDt d
    INNER JOIN dbo.FINAPApprovalHd h ON h.TransNmbr = d.TransNmbr
    LEFT JOIN InvoiceSource inv ON inv.InvoiceNo = d.InvoiceNo
    WHERE COALESCE(
            NULLIF(NULLIF(LTRIM(RTRIM(d.PONo)), ''), '-'),
            inv.PONo
        ) IS NOT NULL
      AND ISNULL(h.Status, '') <> 'D'
),
ApprovalByPo AS
(
    SELECT
        PONo,
        SuppCode,
        MAX(ApprovalNo) AS ApprovalNo,
        MAX(ApprovalStatus) AS ApprovalStatus,
        MAX(CASE WHEN NULLIF(ISNULL(ApprovalNo, ''), '') IS NOT NULL THEN 1 ELSE 0 END) AS HasPaymentApproval,
        MAX(CASE WHEN ISNULL(ApprovalStatus, '') = 'P'
                   OR ISNULL(ApprovalDonePayment, 'N') = 'Y'
                  THEN 1 ELSE 0 END) AS HasPostedPaymentApproval
    FROM ApprovalSource
    WHERE PONo IS NOT NULL
    GROUP BY PONo, SuppCode
),
PaymentSource AS
(
    SELECT DISTINCT
        appr.PONo,
        COALESCE(appr.SuppCode, h.SuppCode) AS SuppCode,
        h.TransNmbr AS PaymentNo,
        h.Status AS PaymentStatus
    FROM dbo.FINPayTradeVoucherDt2 d
    INNER JOIN dbo.FINPayTradeVoucherHd h ON h.TransNmbr = d.TransNmbr
    INNER JOIN ApprovalSource appr ON appr.ApprovalNo = d.VoucherNo
    WHERE appr.PONo IS NOT NULL
      AND ISNULL(h.Status, '') <> 'D'
),
PaymentByPo AS
(
    SELECT
        PONo,
        SuppCode,
        MAX(PaymentNo) AS PaymentNo,
        MAX(PaymentStatus) AS PaymentStatus,
        MAX(CASE WHEN NULLIF(ISNULL(PaymentNo, ''), '') IS NOT NULL THEN 1 ELSE 0 END) AS HasPayment,
        MAX(CASE WHEN ISNULL(PaymentStatus, '') = 'P' THEN 1 ELSE 0 END) AS HasPostedPayment
    FROM PaymentSource
    WHERE PONo IS NOT NULL
    GROUP BY PONo, SuppCode
),
PoKeys AS
(
    SELECT PONo, SuppCode FROM RrpoByPo
    UNION
    SELECT PONo, SuppCode FROM InvoiceByPo
    UNION
    SELECT PONo, SuppCode FROM ApprovalByPo
    UNION
    SELECT PONo, SuppCode FROM PaymentByPo
)
SELECT
    po.PONo,
    po.SuppCode,
    rr.RrpoNo,
    inv.InvoiceNo,
    inv.InvoiceStatus,
    appr.ApprovalNo,
    appr.ApprovalStatus,
    pay.PaymentNo,
    pay.PaymentStatus,
    CAST(CASE WHEN rr.PONo IS NOT NULL THEN 1 ELSE 0 END AS bit) AS HasRrpo,
    CAST(CASE WHEN ISNULL(inv.HasInvoice, 0) = 1
               OR ISNULL(inv.HasPostedInvoice, 0) = 1
               OR ISNULL(rr.HasDoneInvoiceFlag, 0) = 1
              THEN 1 ELSE 0 END AS bit) AS IsDoneInvoice,
    CAST(CASE WHEN ISNULL(appr.HasPaymentApproval, 0) = 1
               OR ISNULL(pay.HasPayment, 0) = 1
              THEN 1 ELSE 0 END AS bit) AS IsPaymentProcess,
    CAST(CASE WHEN ISNULL(pay.HasPostedPayment, 0) = 1 THEN 1 ELSE 0 END AS bit) AS IsPaymentDone,
    CASE
        WHEN ISNULL(pay.HasPostedPayment, 0) = 1 THEN 'completed'
        WHEN ISNULL(appr.HasPaymentApproval, 0) = 1 OR ISNULL(pay.HasPayment, 0) = 1 THEN 'payment_process'
        WHEN ISNULL(inv.HasInvoice, 0) = 1 OR ISNULL(inv.HasPostedInvoice, 0) = 1 OR ISNULL(rr.HasDoneInvoiceFlag, 0) = 1 THEN 'done_invoice'
        WHEN rr.PONo IS NOT NULL THEN 'rrpo'
        ELSE 'open'
    END AS PoWorkflowStatusKey,
    CASE
        WHEN ISNULL(pay.HasPostedPayment, 0) = 1 THEN 'Selesai'
        WHEN ISNULL(appr.HasPaymentApproval, 0) = 1 OR ISNULL(pay.HasPayment, 0) = 1 THEN 'Dalam Proses Pembayaran'
        WHEN ISNULL(inv.HasInvoice, 0) = 1 OR ISNULL(inv.HasPostedInvoice, 0) = 1 OR ISNULL(rr.HasDoneInvoiceFlag, 0) = 1 THEN 'Done Invoice'
        WHEN rr.PONo IS NOT NULL THEN 'RRPO Dibuat'
        ELSE 'Siap RRPO'
    END AS PoWorkflowStatusLabel
FROM PoKeys po
INNER JOIN dbo.PRCPOHd poHead ON poHead.TransNmbr = po.PONo
    AND poHead.Supplier = po.SuppCode
LEFT JOIN RrpoByPo rr ON rr.PONo = po.PONo AND rr.SuppCode = po.SuppCode
LEFT JOIN InvoiceByPo inv ON inv.PONo = po.PONo AND inv.SuppCode = po.SuppCode
LEFT JOIN ApprovalByPo appr ON appr.PONo = po.PONo AND appr.SuppCode = po.SuppCode
LEFT JOIN PaymentByPo pay ON pay.PONo = po.PONo AND pay.SuppCode = po.SuppCode;
