package com.woody.backend.infraestructure.entity;

import java.time.LocalDate;

import jakarta.persistence.Entity;
import jakarta.persistence.GeneratedValue;
import jakarta.persistence.GenerationType;
import jakarta.persistence.Id;
import jakarta.persistence.ManyToOne;

import com.woody.backend.infraestructure.entity.enums.ReportStatus;

@Entity
public class PostReportEntity {
    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private long id;

    @ManyToOne
    private PostEntity post;

    @ManyToOne
    private UserEntity reporter;

    private String reason;

    private ReportStatus status;

    private LocalDate created_at;

    public PostReportEntity(){}

    public PostReportEntity(PostEntity post, UserEntity reporter, String reason, ReportStatus status,
            LocalDate created_at) {
        this.post = post;
        this.reporter = reporter;
        this.reason = reason;
        this.status = status;
        this.created_at = created_at;
    }

    public PostEntity getPost() {
        return post;
    }

    public void setPost(PostEntity post) {
        this.post = post;
    }

    public UserEntity getReporter() {
        return reporter;
    }

    public void setReporter(UserEntity reporter) {
        this.reporter = reporter;
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
