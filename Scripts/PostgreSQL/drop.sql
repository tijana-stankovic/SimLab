-- Drop tables (indexes are dropped automatically with their tables)
DROP TABLE IF EXISTS simunit_data;
DROP TABLE IF EXISTS simunit;
DROP TABLE IF EXISTS global_data;
DROP TABLE IF EXISTS cycle;
DROP TABLE IF EXISTS phase_parameter;
DROP TABLE IF EXISTS phase;
DROP TABLE IF EXISTS characteristic;
DROP TABLE IF EXISTS global_initial;
DROP TABLE IF EXISTS global_characteristic;
DROP TABLE IF EXISTS world;
DROP FUNCTION IF EXISTS trg_world_set_uid();
