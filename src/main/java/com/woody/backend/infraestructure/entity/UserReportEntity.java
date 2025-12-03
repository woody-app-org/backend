package com.woody.backend.infraestructure.entity;

import java.time.LocalDate;

import jakarta.persistence.Entity;
import jakarta.persistence.GeneratedValue;
import jakarta.persistence.GenerationType;
import jakarta.persistence.Id;
import jakarta.persistence.ManyToOne;

import com.woody.backend.infraestructure.entity.enums.ReportStatus;

@Entity
public class UserReportEntity {
    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private long id;

    @ManyToOne
    private UserEntity reportedUser;

    @ManyToOne
    private UserEntity reportingUser;

    private String reason;

    private ReportStatus status;
    
    private LocalDate created_at;

    public UserReportEntity() {}

    public UserReportEntity(UserEntity reportedUser, UserEntity reportingUser, String reason, ReportStatus status,
            LocalDate created_at) {
        this.reportedUser = reportedUser;
        this.reportingUser = reportingUser;
        this.reason = reason;
        this.status = status;
        this.created_at = created_at;
    }

    public UserEntity getReportedUser() {
        return reportedUser;
    }

    public void setReportedUser(UserEntity reportedUser) {
        this.reportedUser = reportedUser;
    }

    public UserEntity getReportingUser() {
        return reportingUser;
    }

    public void setReportingUser(UserEntity reportingUser) {
        this.reportingUser = reportingUser;
    }

    public String getReason() {
        return reason;
    }

    public void setReason(String reason) {
        this.reason = reason;
    }

    public ReportStatus getStatus() {
        return status;
    }

    public void setStatus(ReportStatus status) {
        this.status = status;
    }

    public LocalDate getCreated_at() {
        return created_at;
    }

    public void setCreated_at(LocalDate created_at) {
        this.created_at = created_at;
    }

    public long getId() {
        return id;
    }
}
