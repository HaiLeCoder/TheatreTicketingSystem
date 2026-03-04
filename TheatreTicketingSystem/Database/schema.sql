-- ============================================================
-- Theatre Ticketing System - Safe CREATE Schema
-- Version: 1.1.0 (Post RC-019 Fix)
-- Purpose: Idempotent schema creation – safe to run on production.
--          Use schema_reset.sql ONLY in dev/test to wipe data.
-- RC-019 FIX: Removed DROP TABLE CASCADE statements.
--             Uses CREATE TABLE IF NOT EXISTS instead.
-- ============================================================

-- ============================================================
-- TABLE: seat_types
-- ============================================================
CREATE TABLE IF NOT EXISTS seat_types (
    id          SERIAL PRIMARY KEY,
    name        VARCHAR(50)     NOT NULL UNIQUE,
    price       NUMERIC(10, 2)  NOT NULL,
    description TEXT,
    created_at  TIMESTAMPTZ     NOT NULL DEFAULT NOW()
);

-- Seed default seat types (idempotent with ON CONFLICT DO NOTHING)
INSERT INTO seat_types (name, price, description) VALUES
    ('Ghế thường', 150000,  'Ghế ngồi tiêu chuẩn'),
    ('Ghế VIP',    350000,  'Ghế hạng VIP, view tốt, rộng rãi'),
    ('Ghế đôi',    500000,  'Ghế đôi dành cho 2 người')
ON CONFLICT (name) DO NOTHING;

-- ============================================================
-- TABLE: performances
-- ============================================================
CREATE TABLE IF NOT EXISTS performances (
    id              SERIAL PRIMARY KEY,
    name            VARCHAR(200)    NOT NULL,
    start_time      TIMESTAMPTZ     NOT NULL,
    duration_minutes INT            NOT NULL CHECK (duration_minutes > 0),
    location        VARCHAR(200),
    description     TEXT,
    total_seats     INT             NOT NULL DEFAULT 100
                        CHECK (total_seats > 0 AND total_seats <= 100),
    is_active       BOOLEAN         NOT NULL DEFAULT TRUE,
    created_at      TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ     NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_performances_start_time ON performances (start_time);
CREATE INDEX IF NOT EXISTS idx_performances_name
    ON performances USING gin(to_tsvector('simple', name));

-- ============================================================
-- TABLE: bookings
-- ============================================================
CREATE TABLE IF NOT EXISTS bookings (
    id              SERIAL PRIMARY KEY,
    performance_id  INT             NOT NULL REFERENCES performances(id) ON DELETE RESTRICT,
    seat_type_id    INT             NOT NULL REFERENCES seat_types(id)   ON DELETE RESTRICT,
    customer_name   VARCHAR(200)    NOT NULL,
    customer_phone  VARCHAR(20),
    ticket_count    INT             NOT NULL CHECK (ticket_count > 0),
    total_amount    NUMERIC(10, 2)  NOT NULL,
    status          VARCHAR(20)     NOT NULL DEFAULT 'PENDING'
                        CHECK (status IN ('PENDING', 'CONFIRMED', 'CANCELLED')),
    notes           TEXT,
    created_at      TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ     NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_bookings_performance_id ON bookings (performance_id);
CREATE INDEX IF NOT EXISTS idx_bookings_status         ON bookings (status);

-- ============================================================
-- TABLE: seat_assignments
-- ============================================================
CREATE TABLE IF NOT EXISTS seat_assignments (
    id              SERIAL PRIMARY KEY,
    booking_id      INT             NOT NULL REFERENCES bookings(id)    ON DELETE CASCADE,
    performance_id  INT             NOT NULL REFERENCES performances(id) ON DELETE RESTRICT,
    row_label       CHAR(1)         NOT NULL CHECK (row_label BETWEEN 'A' AND 'J'),
    col_number      INT             NOT NULL CHECK (col_number BETWEEN 1 AND 10),
    assigned_at     TIMESTAMPTZ     NOT NULL DEFAULT NOW(),

    CONSTRAINT uq_seat_per_performance UNIQUE (performance_id, row_label, col_number)
);

CREATE INDEX IF NOT EXISTS idx_seat_assignments_booking_id     ON seat_assignments (booking_id);
CREATE INDEX IF NOT EXISTS idx_seat_assignments_performance_id ON seat_assignments (performance_id);

-- ============================================================
-- TRIGGER: auto-update updated_at
-- ============================================================
CREATE OR REPLACE FUNCTION fn_set_updated_at()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = NOW();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_trigger
                   WHERE tgname = 'trg_performances_updated_at') THEN
        CREATE TRIGGER trg_performances_updated_at
            BEFORE UPDATE ON performances
            FOR EACH ROW EXECUTE FUNCTION fn_set_updated_at();
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_trigger
                   WHERE tgname = 'trg_bookings_updated_at') THEN
        CREATE TRIGGER trg_bookings_updated_at
            BEFORE UPDATE ON bookings
            FOR EACH ROW EXECUTE FUNCTION fn_set_updated_at();
    END IF;
END$$;

-- ============================================================
-- VIEWS
-- ============================================================
CREATE OR REPLACE VIEW vw_booking_summary AS
SELECT
    b.id            AS booking_id,
    p.name          AS performance_name,
    p.start_time,
    st.name         AS seat_type,
    st.price        AS unit_price,
    b.customer_name,
    b.customer_phone,
    b.ticket_count,
    b.total_amount,
    b.status,
    (SELECT COUNT(*) FROM seat_assignments sa WHERE sa.booking_id = b.id) AS seats_assigned,
    b.created_at
FROM bookings b
JOIN performances  p  ON p.id  = b.performance_id
JOIN seat_types    st ON st.id = b.seat_type_id;

CREATE OR REPLACE VIEW vw_seat_occupancy AS
SELECT
    sa.performance_id,
    sa.row_label,
    sa.col_number,
    sa.booking_id,
    b.customer_name,
    b.status AS booking_status
FROM seat_assignments sa
JOIN bookings b ON b.id = sa.booking_id;

-- ============================================================
-- SAMPLE DATA (for initial setup – idempotent)
-- ============================================================
INSERT INTO performances (name, start_time, duration_minutes, location, description, total_seats)
SELECT 'Vở kịch Dế Mèn Phiêu Lưu Ký', NOW() + INTERVAL '3 days',  90, 'Nhà hát Lớn Hà Nội',   'Chuyển thể từ tác phẩm của Tô Hoài', 100
WHERE NOT EXISTS (SELECT 1 FROM performances WHERE name = 'Vở kịch Dế Mèn Phiêu Lưu Ký');

INSERT INTO performances (name, start_time, duration_minutes, location, description, total_seats)
SELECT 'Nhạc kịch Mamma Mia', NOW() + INTERVAL '7 days', 120, 'Nhà hát Tuổi Trẻ', 'Nhạc kịch nổi tiếng thế giới', 100
WHERE NOT EXISTS (SELECT 1 FROM performances WHERE name = 'Nhạc kịch Mamma Mia');

INSERT INTO performances (name, start_time, duration_minutes, location, description, total_seats)
SELECT 'Hài kịch Táo Quân 2026', NOW() + INTERVAL '14 days', 150, 'Cung Văn hoá Lao động', 'Chương trình hài kịch thường niên', 100
WHERE NOT EXISTS (SELECT 1 FROM performances WHERE name = 'Hài kịch Táo Quân 2026');
