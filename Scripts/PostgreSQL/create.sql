-- Create world table (simulation world definitions loaded from JSON configuration)
CREATE TABLE world (
    id          integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    uid         text NOT NULL,
    name        text NOT NULL,
    space       smallint NOT NULL CHECK (space IN (1, 2, 3)),
    x           integer NOT NULL CHECK (x >= 0), -- 0 means unbounded dimension
    y           integer NOT NULL CHECK (y >= 0), -- 0 = unbounded / unused in 1D
    z           integer NOT NULL DEFAULT 0 CHECK (z >= 0), -- 0 = unbounded / unused in 1D & 2D
    mode        char(1) NOT NULL CHECK (mode IN ('S', 'A')), -- S = SynchronousCA, A = Asynchronous
    last_cycle  bigint CHECK (last_cycle IS NULL OR last_cycle >= 0), -- null = simulation not initialized yet
    next_cell_id bigint NOT NULL DEFAULT 1 CHECK (next_cell_id >= 1), -- next value for system _id assignment
    last_viewed_frame bigint CHECK (last_viewed_frame IS NULL OR last_viewed_frame >= 0), -- null = visualization not opened yet
    created_at  timestamptz NOT NULL DEFAULT now(),
    CHECK (
        (space = 1 AND y = 0 AND z = 0) OR
        (space = 2 AND z = 0) OR
        (space = 3)
    )
);

-- Assign UID from generated world ID when UID is not provided.
CREATE OR REPLACE FUNCTION trg_world_set_uid()
RETURNS trigger
LANGUAGE plpgsql
AS $$
BEGIN
    IF NEW.uid IS NULL OR btrim(NEW.uid) = '' THEN
        NEW.uid := NEW.id::text;
    END IF;

    RETURN NEW;
END;
$$;

CREATE TRIGGER bi_world_set_uid
BEFORE INSERT OR UPDATE ON world
FOR EACH ROW
EXECUTE FUNCTION trg_world_set_uid();

-- Enforce case-insensitive uniqueness for world UID while preserving original text
CREATE UNIQUE INDEX ux_world_uid_ci ON world (upper(uid));

-- Create global_characteristic table (ordered global characteristic definitions per world)
CREATE TABLE global_characteristic (
    id      integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    world   integer NOT NULL REFERENCES world(id) ON DELETE CASCADE,
    name    text NOT NULL,
    ord     integer NOT NULL CHECK (ord >= 0) -- zero-based order to match C# array/list indexing
);

-- Enforce case-insensitive uniqueness for global characteristic name within a world
CREATE UNIQUE INDEX ux_global_characteristic_world_name_ci ON global_characteristic (world, upper(name));

-- Enforce unique order position of global characteristics within a world
CREATE UNIQUE INDEX ux_global_characteristic_world_ord ON global_characteristic (world, ord);

-- Create global_initial table (initial global values before the first saved cycle)
CREATE TABLE global_initial (
    world                   integer NOT NULL REFERENCES world(id) ON DELETE CASCADE,
    global_characteristic   integer NOT NULL REFERENCES global_characteristic(id) ON DELETE CASCADE,
    value                   real NOT NULL,
    PRIMARY KEY (world, global_characteristic)
);

CREATE INDEX ix_global_initial_global_characteristic ON global_initial (global_characteristic);

-- Create characteristic table (ordered cell characteristic definitions per world)
CREATE TABLE characteristic (
    id      integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    world   integer NOT NULL REFERENCES world(id) ON DELETE CASCADE,
    name    text NOT NULL,
    ord     integer NOT NULL CHECK (ord >= 0) -- zero-based order to match C# array/list indexing
);

-- Enforce case-insensitive uniqueness for characteristic name within a world
CREATE UNIQUE INDEX ux_characteristic_world_name_ci ON characteristic (world, upper(name));

-- Enforce unique order position of characteristics within a world
CREATE UNIQUE INDEX ux_characteristic_world_ord ON characteristic (world, ord);

-- Create phase table (phase -> method mapping per world)
CREATE TABLE phase (
    id          integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    world       integer NOT NULL REFERENCES world(id) ON DELETE CASCADE,
    name        text NOT NULL CHECK (upper(name) IN (
                    'INITIALIZATION',
                    'PRECYCLE',
                    'PROCESSWORLD',
                    'UPDATE',
                    'EVALUATION',
                    'REPRODUCTION',
                    'SELECTION',
                    'POSTCYCLE'
                )), -- Supported simulation phases
    method      text NOT NULL  -- "DllName;ClassName;MethodName"
);

-- Enforce case-insensitive uniqueness for phase name within a world
CREATE UNIQUE INDEX ux_phase_world_name_ci ON phase (world, upper(name));

-- Create phase_parameter table (ordered method parameters per phase entry)
CREATE TABLE phase_parameter (
    phase       integer NOT NULL REFERENCES phase(id) ON DELETE CASCADE,
    ord         integer NOT NULL CHECK (ord >= 0), -- zero-based order to match C# array/list indexing
    value       text NOT NULL,
    PRIMARY KEY (phase, ord)
);

-- Create cycle table (one row per saved simulation cycle for a world)
CREATE TABLE cycle (
    id              bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    world           integer NOT NULL REFERENCES world(id) ON DELETE CASCADE,
    cycle           bigint NOT NULL CHECK (cycle >= 0), -- 0 = after initialization
    simunit_count   bigint NOT NULL DEFAULT 0 CHECK (simunit_count >= 0),
    created_at      timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT ux_cycle_world_cycle UNIQUE (world, cycle)
);

CREATE INDEX ix_cycle_world ON cycle (world);

-- Create global_data table (global characteristic values for each cycle)
CREATE TABLE global_data (
    cycle                   bigint NOT NULL REFERENCES cycle(id) ON DELETE CASCADE,
    global_characteristic   integer NOT NULL REFERENCES global_characteristic(id) ON DELETE CASCADE,
    value                   real NOT NULL,
    PRIMARY KEY (cycle, global_characteristic)
);

CREATE INDEX ix_global_data_global_characteristic ON global_data (global_characteristic);

-- Create simunit table (cells/simulation units stored for one cycle)
CREATE TABLE simunit (
    id          bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    cycle       bigint NOT NULL REFERENCES cycle(id) ON DELETE CASCADE,
    simunit_id  bigint NOT NULL CHECK (simunit_id >= 1),
    x           integer NOT NULL,
    y           integer NOT NULL DEFAULT 0,
    z           integer NOT NULL DEFAULT 0,
    CONSTRAINT ux_simunit_cycle_simunit_id UNIQUE (cycle, simunit_id),
    CONSTRAINT ux_simunit_cycle_position UNIQUE (cycle, x, y, z)
);

CREATE INDEX ix_simunit_cycle ON simunit (cycle);

-- Create simunit_data table (characteristic values for each simunit)
CREATE TABLE simunit_data (
    simunit         bigint NOT NULL REFERENCES simunit(id) ON DELETE CASCADE,
    characteristic  integer NOT NULL REFERENCES characteristic(id) ON DELETE CASCADE,
    value           real NOT NULL,
    PRIMARY KEY (simunit, characteristic)
);

CREATE INDEX ix_simunit_data_characteristic ON simunit_data (characteristic);
