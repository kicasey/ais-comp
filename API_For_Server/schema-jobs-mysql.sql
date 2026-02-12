-- Run this on your MySQL (resume_ai database) if jobs/recommendations tables don't exist.
-- resume_pii (resume_id, candidate_name) is assumed to already exist.

USE resume_ai;

CREATE TABLE IF NOT EXISTS jobs (
  id VARCHAR(64) NOT NULL PRIMARY KEY,
  title VARCHAR(255) DEFAULT '',
  department VARCHAR(128) DEFAULT '',
  location VARCHAR(128) DEFAULT '',
  description TEXT
);

CREATE TABLE IF NOT EXISTS job_recommendations (
  job_id VARCHAR(64) NOT NULL,
  resume_id VARCHAR(64) NOT NULL,
  score INT NOT NULL DEFAULT 0,
  PRIMARY KEY (job_id, resume_id),
  KEY idx_job (job_id)
);

-- Example seed (optional):
-- INSERT INTO jobs (id, title, department, location, description) VALUES
-- ('job-1', 'Senior Consultant – Technology', 'Technology', 'New York', 'Lead technology advisory engagements.'),
-- ('job-2', 'Manager – Assurance', 'Assurance', 'Chicago', 'Manage assurance teams and engagements.'),
-- ('job-3', 'Staff – Tax', 'Tax', 'Dallas', 'Support tax compliance and advisory.');
-- INSERT INTO job_recommendations (job_id, resume_id, score) VALUES
-- ('job-3', '<resume_id_from_resume_pii>', 88),
-- ('job-3', '<another_resume_id>', 65);
